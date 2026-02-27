using System.Diagnostics;
using System.Net;
using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;
using Xunit;

namespace Project.E2E.Tests.Infrastructure;

public class E2EEnvironmentFixture : IAsyncLifetime
{
    private const string DbAlias = "postgres-e2e";
    private const string DbName = "ppo_test";
    private const string DbUser = "postgres";
    private const string DbPassword = "postgres";
    private static readonly SemaphoreSlim PublishLock = new(1, 1);

    private PostgreSqlContainer? _postgres;
    private IContainer? _app;
    private string _repoRoot = string.Empty;
    private string _publishDir = string.Empty;

    public string BaseApiUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var stage = "resolve-repository-root";
        var totalWatch = Stopwatch.StartNew();
        var stageWatch = new Stopwatch();

        try
        {
            _repoRoot = ResolveRepositoryRoot();
            _publishDir = Path.Combine(_repoRoot, ".e2e-publish-cache");
            Console.WriteLine($"[E2E] Repo root: {_repoRoot}");
            Console.WriteLine($"[E2E] Publish dir: {_publishDir}");

            stage = "publish-httpserver";
            stageWatch.Restart();
            await PublishHttpServerAsync();
            Console.WriteLine($"[E2E] Stage '{stage}' done in {stageWatch.Elapsed}");

            stage = "start-database";
            stageWatch.Restart();
            await StartDatabaseAsync();
            Console.WriteLine($"[E2E] Stage '{stage}' done in {stageWatch.Elapsed}");

            stage = "seed-database";
            stageWatch.Restart();
            await SeedDatabaseAsync();
            Console.WriteLine($"[E2E] Stage '{stage}' done in {stageWatch.Elapsed}");

            stage = "start-application";
            stageWatch.Restart();
            await StartApplicationAsync();
            Console.WriteLine($"[E2E] Stage '{stage}' done in {stageWatch.Elapsed}");

            stage = "wait-application";
            stageWatch.Restart();
            await WaitForApplicationAsync();
            Console.WriteLine($"[E2E] Stage '{stage}' done in {stageWatch.Elapsed}");

            Console.WriteLine($"[E2E] Environment initialized in {totalWatch.Elapsed}");
        }
        catch (Exception ex)
        {
            var diagnostics = BuildDiagnostics(stage, ex);
            Console.WriteLine(diagnostics);
            throw new InvalidOperationException(diagnostics, ex);
        }
    }

    public async Task ResetDatabaseAsync()
    {
        await ExecPsqlFileAsync("/db-data/integration/truncate.sql");
        await ExecPsqlFileAsync("/db-data/integration/copy_all.sql");
    }

    public async Task<string> GetApplicationLogsAsync()
    {
        if (_app is null)
            return "<app container is not initialized>";

        return await TryGetDockerContainerLogsAsync(_app.Id);
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            try
            {
                await _app.DisposeAsync();
            }
            catch
            {
                // ignored
            }
        }

        if (_postgres is not null)
        {
            try
            {
                await _postgres.DisposeAsync();
            }
            catch
            {
                // ignored
            }
        }

        // Publish cache intentionally persists between test runs to avoid expensive republish.
    }

    private async Task PublishHttpServerAsync()
    {
        var httpServerProject = Path.Combine(_repoRoot, "src", "Project.HttpServer", "Project.HttpServer.csproj");
        var sourceStamp = ComputeSourceStamp();

        await PublishLock.WaitAsync();
        try
        {
            if (IsPublishCacheActual(sourceStamp))
            {
                Console.WriteLine("[E2E] Reusing cached publish output.");
                return;
            }

            if (Directory.Exists(_publishDir))
                Directory.Delete(_publishDir, recursive: true);

            Directory.CreateDirectory(_publishDir);

            var args = $"publish \"{httpServerProject}\" -c Release -o \"{_publishDir}\" --nologo --no-restore";
            await RunProcessAsync("dotnet", args, _repoRoot);
            await File.WriteAllTextAsync(GetPublishStampFilePath(), sourceStamp.ToString());
            Console.WriteLine("[E2E] Publish cache refreshed.");
        }
        finally
        {
            PublishLock.Release();
        }
    }

    private async Task StartDatabaseAsync()
    {
        var dbScriptsPath = Path.Combine(_repoRoot, "DB_data_scripts");

        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase(DbName)
            .WithUsername(DbUser)
            .WithPassword(DbPassword)
            .WithPortBinding(5432, true)
            .WithBindMount(dbScriptsPath, "/db-data", AccessMode.ReadOnly)
            .WithCleanUp(true)
            .WithAutoRemove(true)
            .Build();

        await _postgres.StartAsync();
    }

    private async Task SeedDatabaseAsync()
    {
        await ExecPsqlFileAsync("/db-data/integration/create.sql");
        await ExecPsqlFileAsync("/db-data/integration/truncate.sql");
        await ExecPsqlFileAsync("/db-data/integration/copy_all.sql");
    }

    private async Task StartApplicationAsync()
    {
        if (_postgres is null)
            throw new InvalidOperationException("Postgres container is not initialized");

        if (string.IsNullOrWhiteSpace(_publishDir))
            throw new InvalidOperationException("Publish directory path is not initialized");

        // Defensive guard: Docker bind-mount requires host path to exist.
        Directory.CreateDirectory(_publishDir);

        var dbPort = _postgres.GetMappedPublicPort(5432);
        var connectionString =
            $"User ID={DbUser};Password={DbPassword};Server=host.docker.internal;Port={dbPort};Database={DbName}";

        _app = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/dotnet/aspnet:9.0")
            .WithPortBinding(8080, true)
            .WithBindMount(_publishDir, "/app", AccessMode.ReadWrite)
            .WithWorkingDirectory("/app")
            .WithExtraHost("host.docker.internal", "host-gateway")
            .WithEnvironment("ASPNETCORE_URLS", "http://+:8080")
            .WithEnvironment("ConnectionStrings__DefaultConnection", connectionString)
            .WithCommand("dotnet", "Project.HttpServer.dll")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8080))
            .Build();

        try
        {
            await _app.StartAsync();
        }
        catch (Exception ex)
        {
            var containerLogs = await TryGetDockerContainerLogsAsync(_app.Id);
            throw new InvalidOperationException(
                $"Failed to start HttpServer container.{Environment.NewLine}ContainerId: {_app.Id}{Environment.NewLine}Docker logs:{Environment.NewLine}{containerLogs}",
                ex);
        }

        BaseApiUrl = $"http://localhost:{_app.GetMappedPublicPort(8080)}/api/v1";
    }

    private async Task WaitForApplicationAsync()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        var deadline = DateTimeOffset.UtcNow.AddSeconds(60);
        Exception? lastError = null;
        HttpStatusCode? lastStatus = null;
        string? lastBody = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                var response = await client.GetAsync($"{BaseApiUrl}/companies");
                lastStatus = response.StatusCode;
                lastBody = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == HttpStatusCode.OK)
                    return;
            }
            catch (Exception ex)
            {
                lastError = ex;
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException(
            $"HttpServer container is not ready. Last status: {lastStatus}; Last body: {lastBody}; Last error: {lastError?.Message}");
    }

    private async Task ExecPsqlFileAsync(string filePathInContainer)
    {
        if (_postgres is null)
            throw new InvalidOperationException("Postgres container is not initialized");

        var result = await _postgres.ExecAsync(new[]
        {
            "psql",
            "-U",
            DbUser,
            "-d",
            DbName,
            "-f",
            filePathInContainer
        });

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Failed to execute {filePathInContainer}. ExitCode={result.ExitCode}. Stdout={result.Stdout}. Stderr={result.Stderr}");
        }
    }

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "DB_data_scripts")))
                return current.FullName;
            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not resolve repository root with DB_data_scripts");
    }

    private long ComputeSourceStamp()
    {
        var srcRoot = Path.Combine(_repoRoot, "src");
        if (!Directory.Exists(srcRoot))
            return 0;

        long maxTicks = 0;
        foreach (var file in Directory.EnumerateFiles(srcRoot, "*", SearchOption.AllDirectories))
        {
            if (!file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) &&
                !file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) &&
                !file.EndsWith(".props", StringComparison.OrdinalIgnoreCase) &&
                !file.EndsWith(".targets", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var ticks = File.GetLastWriteTimeUtc(file).Ticks;
            if (ticks > maxTicks)
                maxTicks = ticks;
        }

        return maxTicks;
    }

    private bool IsPublishCacheActual(long sourceStamp)
    {
        if (!Directory.Exists(_publishDir))
            return false;

        var entryPoint = Path.Combine(_publishDir, "Project.HttpServer.dll");
        var stampFile = GetPublishStampFilePath();
        if (!File.Exists(entryPoint) || !File.Exists(stampFile))
            return false;

        var stampText = File.ReadAllText(stampFile).Trim();
        return long.TryParse(stampText, out var cachedStamp) && cachedStamp == sourceStamp;
    }

    private string GetPublishStampFilePath() => Path.Combine(_publishDir, ".source-stamp");

    private static async Task RunProcessAsync(string fileName, string arguments, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        await process.WaitForExitAsync(cts.Token);
        await Task.WhenAll(stdOutTask, stdErrTask);

        var stdOut = stdOutTask.Result;
        var stdErr = stdErrTask.Result;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Command failed: {fileName} {arguments}{Environment.NewLine}STDOUT:{Environment.NewLine}{stdOut}{Environment.NewLine}STDERR:{Environment.NewLine}{stdErr}");
        }
    }

    private async Task<string> TryGetDockerContainerLogsAsync(string containerId)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"logs --tail 200 {containerId}",
            WorkingDirectory = _repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        try
        {
            using var process = new Process { StartInfo = startInfo };
            process.Start();
            var stdOutTask = process.StandardOutput.ReadToEndAsync();
            var stdErrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            await Task.WhenAll(stdOutTask, stdErrTask);

            var stdout = stdOutTask.Result;
            var stderr = stdErrTask.Result;
            if (string.IsNullOrWhiteSpace(stdout) && string.IsNullOrWhiteSpace(stderr))
                return "<empty>";

            return $"STDOUT:{Environment.NewLine}{stdout}{Environment.NewLine}STDERR:{Environment.NewLine}{stderr}";
        }
        catch (Exception ex)
        {
            return $"<failed to read docker logs: {ex.GetType().Name}: {ex.Message}>";
        }
    }

    private string BuildDiagnostics(string stage, Exception ex)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[E2E] Initialization failed.");
        sb.AppendLine($"[E2E] Stage: {stage}");
        sb.AppendLine($"[E2E] Exception: {ex.GetType().Name}: {ex.Message}");
        sb.AppendLine($"[E2E] BaseApiUrl: {BaseApiUrl}");
        sb.AppendLine($"[E2E] PublishDir exists: {(!string.IsNullOrWhiteSpace(_publishDir) && Directory.Exists(_publishDir))}");
        sb.AppendLine($"[E2E] RepoRoot exists: {(!string.IsNullOrWhiteSpace(_repoRoot) && Directory.Exists(_repoRoot))}");
        sb.AppendLine($"[E2E] Postgres created: {_postgres is not null}");
        sb.AppendLine($"[E2E] App created: {_app is not null}");
        return sb.ToString();
    }
}

