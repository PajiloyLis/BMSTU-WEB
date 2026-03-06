using Database.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Project.E2E.Tests.Infrastructure;

public class E2EEnvironmentFixture : IAsyncLifetime
{
    private const string BaseUrlEnv = "E2E_BASE_URL";
    private const string DbHostEnv = "E2E_DB_HOST";
    private const string DbHostPortEnv = "E2E_DB_HOST_PORT";
    private const string DbNameEnv = "E2E_DB_NAME";
    private const string DbUserEnv = "E2E_DB_USER";
    private const string DbPasswordEnv = "E2E_DB_PASSWORD";

    private const string DefaultDbHost = "localhost";
    private const int DefaultDbHostPort = 55433;
    private const string DefaultDbName = "e2e_external";
    private const string DefaultDbUser = "postgres";
    private const string DefaultDbPassword = "postgres";

    private string _connectionString = string.Empty;
    private string _repoRoot = string.Empty;
    private string _createScriptPath = string.Empty;
    private string _truncateScriptPath = string.Empty;
    private string _copyScriptPath = string.Empty;

    public string BaseApiUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _repoRoot = ResolveRepositoryRoot();
        _createScriptPath = Path.Combine(_repoRoot, "DB_data_scripts", "integration", "create.sql");
        _truncateScriptPath = Path.Combine(_repoRoot, "DB_data_scripts", "integration", "truncate.sql");
        _copyScriptPath = Path.Combine(_repoRoot, "DB_data_scripts", "integration", "copy_all.sql");
        _connectionString = BuildConnectionString();
        BaseApiUrl = ResolveBaseUrl();

        await WaitForDatabaseReadyAsync();
        await ExecuteScriptAsync(_createScriptPath);
        await ResetDatabaseAsync();
        await EnsureExternalApplicationIsReachableAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await ExecuteScriptAsync(_truncateScriptPath);
        await ExecuteScriptAsync(_copyScriptPath);
    }

    public Task<string> GetApplicationLogsAsync()
    {
        return Task.FromResult("<app logs are unavailable in external e2e mode>");
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private static string ResolveBaseUrl()
    {
        var raw = Environment.GetEnvironmentVariable(BaseUrlEnv);
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException($"{BaseUrlEnv} is not set. E2E tests are external-only.");

        var normalized = raw.Trim();
        return normalized.EndsWith("/", StringComparison.Ordinal) ? normalized : $"{normalized}/";
    }

    private static string BuildConnectionString()
    {
        var host = ReadEnvOrDefault(DbHostEnv, DefaultDbHost);
        var port = ResolveDbHostPort();
        var dbName = ReadEnvOrDefault(DbNameEnv, DefaultDbName);
        var dbUser = ReadEnvOrDefault(DbUserEnv, DefaultDbUser);
        var dbPassword = ReadEnvOrDefault(DbPasswordEnv, DefaultDbPassword);
        return $"Host={host};Port={port};Database={dbName};Username={dbUser};Password={dbPassword}";
    }

    private static int ResolveDbHostPort()
    {
        var raw = Environment.GetEnvironmentVariable(DbHostPortEnv);
        if (string.IsNullOrWhiteSpace(raw))
            return DefaultDbHostPort;

        if (!int.TryParse(raw, out var port) || port < 1 || port > 65535)
        {
            throw new InvalidOperationException(
                $"Environment variable {DbHostPortEnv} has invalid value '{raw}'. Expected integer in range 1..65535.");
        }

        return port;
    }

    private static string ReadEnvOrDefault(string key, string defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(raw) ? defaultValue : raw.Trim();
    }

    private ProjectDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProjectDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        return new ProjectDbContext(options);
    }

    private async Task ExecuteScriptAsync(string scriptPath)
    {
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException($"SQL script not found: {scriptPath}");

        var sql = await File.ReadAllTextAsync(scriptPath);
        await using var context = CreateDbContext();
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
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

    private async Task WaitForDatabaseReadyAsync()
    {
        var lastError = string.Empty;
        for (var i = 0; i < 60; i++)
        {
            try
            {
                await using var context = CreateDbContext();
                await context.Database.OpenConnectionAsync();
                await context.Database.CloseConnectionAsync();
                return;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                await Task.Delay(1000);
            }
        }

        throw new InvalidOperationException(
            $"E2E database is not reachable using env '{DbHostEnv}:{DbHostPortEnv}/{DbNameEnv}'. Last error: {lastError}");
    }

    private async Task EnsureExternalApplicationIsReachableAsync()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        Exception? lastException = null;

        for (var i = 0; i < 60; i++)
        {
            try
            {
                using var response = await client.GetAsync($"{BaseApiUrl}api/v1/companies");
                if (response.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Unauthorized)
                    return;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            await Task.Delay(1000);
        }

        throw new InvalidOperationException(
            $"E2E app at '{BaseApiUrl}' is not reachable. Set {BaseUrlEnv} to running app-under-test URL.",
            lastException);
    }
}

