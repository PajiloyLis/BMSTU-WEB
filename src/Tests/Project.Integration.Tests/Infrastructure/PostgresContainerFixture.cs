using DotNet.Testcontainers.Configurations;
using Database.Context;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Project.Integration.Tests.Infrastructure;

public class PostgresContainerFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private string _repoRoot = string.Empty;
    private string _dbScriptsDirectory = string.Empty;
    private string _createScriptPath = string.Empty;
    private string _truncateScriptPath = string.Empty;
    private string _copyScriptPath = string.Empty;

    public string ConnectionString { get; private set; } = string.Empty;

    public static readonly Guid SeedEmployeeId = Guid.Parse("bad8a5a0-ec08-412e-8f19-0d9e993d5651");

    public async Task InitializeAsync()
    {
        _repoRoot = ResolveRepositoryRoot();
        _dbScriptsDirectory = Path.Combine(_repoRoot, "DB_data_scripts");
        _createScriptPath = Path.Combine(_dbScriptsDirectory, "integration", "create.sql");
        _truncateScriptPath = Path.Combine(_dbScriptsDirectory, "integration", "truncate.sql");
        _copyScriptPath = Path.Combine(_dbScriptsDirectory, "integration", "copy_all.sql");

        var dbName = $"integration_{Guid.NewGuid():N}";
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithDatabase(dbName)
            .WithBindMount(_dbScriptsDirectory, "/db-data", AccessMode.ReadOnly)
            .WithCleanUp(true)
            .WithAutoRemove(true)
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

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
        if (_container is null)
            return;

        try
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
        catch (Exception e)
        {
            // В некоторых локальных окружениях Docker может не дать удалить контейнер без root.
            // Это не должно помечать integration-тесты как failed после успешного выполнения.
            Console.WriteLine($"[WARN] Failed to dispose testcontainer: {e.Message}");
        }
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
}

