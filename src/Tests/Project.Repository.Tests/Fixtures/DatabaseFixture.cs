using Database.Context;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Project.Repository.Tests.Fixtures;

/// <summary>
/// Фикстура для интеграционных тестов репозиториев.
/// Поднимает PostgreSQL в контейнере, применяет миграции
/// и предоставляет ProjectDbContext для всех тестов в коллекции.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;

    public ProjectDbContext Context { get; private set; } = null!;

    public DatabaseFixture()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("repo_test")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .WithAutoRemove(true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilPortIsAvailable(5432))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var options = new DbContextOptionsBuilder<ProjectDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        Context = new ProjectDbContext(options);
        await Context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (Context != null)
            await Context.DisposeAsync();

        await _postgresContainer.DisposeAsync();
    }

    /// <summary>
    /// Создаёт новый DbContext для того же контейнера (для изоляции трекера).
    /// </summary>
    public ProjectDbContext CreateFreshContext()
    {
        var options = new DbContextOptionsBuilder<ProjectDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        return new ProjectDbContext(options);
    }

    /// <summary>
    /// Очищает все таблицы базы данных для изоляции тестов.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await Context.Database.ExecuteSqlRawAsync(@"
            DELETE FROM ""score_story"";
            DELETE FROM ""post_history"";
            DELETE FROM ""position_history"";
            DELETE FROM ""education"";
            DELETE FROM ""post"";
            DELETE FROM ""position"";
            DELETE FROM ""employee_base"";
            DELETE FROM ""company"";
        ");
        Context.ChangeTracker.Clear();
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}

