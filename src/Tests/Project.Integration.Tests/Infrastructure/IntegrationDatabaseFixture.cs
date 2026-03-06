using Database.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Project.Integration.Tests.Infrastructure;

public class IntegrationDatabaseFixture : IAsyncLifetime
{
    private const string DbHostEnv = "INTEGRATION_DB_HOST";
    private const string DbHostPortEnv = "INTEGRATION_DB_HOST_PORT";
    private const string DbNameEnv = "INTEGRATION_DB_NAME";
    private const string DbUserEnv = "INTEGRATION_DB_USER";
    private const string DbPasswordEnv = "INTEGRATION_DB_PASSWORD";

    private const string DefaultDbHost = "localhost";
    private const int DefaultDbHostPort = 55432;
    private const string DefaultDbName = "integration_external";
    private const string DefaultDbUser = "postgres";
    private const string DefaultDbPassword = "postgres";

    private string _repoRoot = string.Empty;
    private string _dbScriptsDirectory = string.Empty;
    private string _createScriptPath = string.Empty;
    private string _truncateScriptPath = string.Empty;
    private string _copyScriptPath = string.Empty;

    public string ConnectionString { get; private set; } = string.Empty;
    public int ExposedDbHostPort { get; private set; }

    public static readonly Guid SeedEmployeeId = Guid.Parse("bad8a5a0-ec08-412e-8f19-0d9e993d5651");

    public async Task InitializeAsync()
    {
        _repoRoot = ResolveRepositoryRoot();
        _dbScriptsDirectory = Path.Combine(_repoRoot, "DB_data_scripts");
        _createScriptPath = Path.Combine(_dbScriptsDirectory, "integration", "create.sql");
        _truncateScriptPath = Path.Combine(_dbScriptsDirectory, "integration", "truncate.sql");
        _copyScriptPath = Path.Combine(_dbScriptsDirectory, "integration", "copy_all.sql");

        ConnectionString = BuildConnectionString();
        ExposedDbHostPort = ResolveDbHostPort();
        await WaitForDatabaseReadyAsync();

        await using var context = CreateDbContext();
        await ExecuteScriptAsync(context, _createScriptPath);

        await ResetToBaselineAsync();
    }

    public async Task ResetToBaselineAsync()
    {
        await using var context = CreateDbContext();
        await ExecuteScriptAsync(context, _truncateScriptPath);
        await ExecuteScriptAsync(context, _copyScriptPath);
    }

    public async Task DisposeAsync()
    {
        await Task.CompletedTask;
    }

    private ProjectDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProjectDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new ProjectDbContext(options);
    }

    private static async Task ExecuteScriptAsync(ProjectDbContext context, string scriptPath)
    {
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException($"SQL script not found: {scriptPath}");

        var sql = await File.ReadAllTextAsync(scriptPath);
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

        throw new DirectoryNotFoundException("Could not resolve repository root with DB_data_scripts directory");
    }

    private static int ResolveDbHostPort()
    {
        var rawValue = Environment.GetEnvironmentVariable(DbHostPortEnv);
        if (string.IsNullOrWhiteSpace(rawValue))
            return DefaultDbHostPort;

        if (!int.TryParse(rawValue, out var parsedPort) || parsedPort < 1 || parsedPort > 65535)
        {
            throw new InvalidOperationException(
                $"Environment variable {DbHostPortEnv} has invalid value '{rawValue}'. " +
                "Expected integer in range 1..65535.");
        }

        return parsedPort;
    }

    private static string BuildConnectionString()
    {
        var host = ReadEnvOrDefault(DbHostEnv, DefaultDbHost);
        var port = ResolveDbHostPort();
        var database = ReadEnvOrDefault(DbNameEnv, DefaultDbName);
        var user = ReadEnvOrDefault(DbUserEnv, DefaultDbUser);
        var password = ReadEnvOrDefault(DbPasswordEnv, DefaultDbPassword);

        return $"Host={host};Port={port};Database={database};Username={user};Password={password}";
    }

    private static string ReadEnvOrDefault(string key, string defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(raw) ? defaultValue : raw.Trim();
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
            $"Test database is not reachable using env '{DbHostEnv}:{DbHostPortEnv}/{DbNameEnv}'. Last error: {lastError}");
    }
}
