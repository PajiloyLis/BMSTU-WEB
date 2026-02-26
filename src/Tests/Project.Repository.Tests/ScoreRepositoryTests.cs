using Database.Models;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Core.Exceptions;
using Project.Core.Models.Score;
using Project.Database.Models;
using Project.Repository.Tests.Factories;
using Project.Repository.Tests.Fixtures;
using Xunit;

namespace Project.Repository.Tests;

[Collection("Database")]
public class ScoreRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly ScoreRepository _repository;

    private Guid _employeeId;
    private Guid _employeeId2;
    private Guid _authorId;
    private Guid _directorId;
    private Guid _positionId;
    private Guid _lowestPositionId;
    private Guid _managerPositionId;

    public ScoreRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new ScoreRepository(
            _fixture.Context,
            NullLogger<ScoreRepository>.Instance);
    }
    
    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

        var company = new CompanyDb(Guid.NewGuid(),
            "Score Test Company",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            "+3234567890", "score_test@example.com",
            "3234567890", "323456789", "3234567890123", "Score Address");
        await _fixture.Context.CompanyDb.AddAsync(company);

        _employeeId = Guid.NewGuid();
        _employeeId2 = Guid.NewGuid();
        _authorId = Guid.NewGuid();
        _directorId = Guid.NewGuid();
        
        var employee = new EmployeeDb(_employeeId, "John Score", "+3234567890",
            "john.score@example.com", new DateOnly(1990, 1, 1), "johnphoto.jpg",
            "{\"Developer\": true}");
        var employee2 = new EmployeeDb(_employeeId2, "Jason Score", "+3234567899",
            "jason.score@example.com", new DateOnly(1991, 1, 1), "jasonphoto.jpg",
            "{\"Developer\": true}");
        var author = new EmployeeDb(_authorId, "Jack Score", "+3234467890",
            "jack.score@example.com", new DateOnly(1989, 1, 1), "jackphoto.jpg",
            "{\"HR Manager\": true}");
        var director = new EmployeeDb(_directorId, "George Score", "+93234567890",
            "george.score@example.com", new DateOnly(1995, 2, 1), "georgephoto.jpg", null);
        await _fixture.Context.EmployeeDb.AddRangeAsync(employee, employee2, director, author);

        _managerPositionId = Guid.NewGuid();
        _positionId = Guid.NewGuid();
        _lowestPositionId = Guid.NewGuid();
        
        var positions = new List<PositionDb>
        {
            new(_managerPositionId, null, "Manager", company.Id),
            new(_positionId, _managerPositionId, "Developer", company.Id),
            new(_lowestPositionId, _positionId, "Junior Developer", company.Id)
        };
        await _fixture.Context.PositionDb.AddRangeAsync(positions);

        var positionsHistories = new List<PositionHistoryDb>
        {
            new(_lowestPositionId, _employeeId2, new DateOnly(2015, 9, 10)),
            new(_positionId, _employeeId, new DateOnly(2016, 9, 10)),
            new(_managerPositionId, _authorId, new DateOnly(2014, 9, 10))
        };
        await _fixture.Context.PositionHistoryDb.AddRangeAsync(positionsHistories);

        await _fixture.Context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ===== AddScoreAsync =====
    
    [Fact]
    public async Task AddScoreAsync_ShouldAddScore_WhenValid()
    {
        // Arrange
        var score = new CreateScore(_employeeId, _authorId, _positionId, 
            DateTimeOffset.UtcNow, 4, 5, 3);

        // Act
        var result = await _repository.AddScoreAsync(score);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.EfficiencyScore);
        Assert.Equal(_employeeId, result.EmployeeId);

        var dbRecord = await _fixture.Context.ScoreDb.FindAsync(result.Id);
        Assert.NotNull(dbRecord);
    }

    [Fact]
    public async Task AddScoreAsync_ShouldThrow_WhenInvalidScores()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _repository.AddScoreAsync(new CreateScore(
                _employeeId, _authorId, _positionId,
            DateTimeOffset.UtcNow, 6, 0, 10)));
    }

    // ===== GetScoreByIdAsync =====

    [Fact]
    public async Task GetScoreByIdAsync_ShouldReturnScore_WhenExists()
    {
        // Arrange
        var score = new ScoreDb(Guid.NewGuid(), _employeeId, _authorId, _positionId,
            DateTimeOffset.UtcNow, 1, 2, 3);
        await _fixture.Context.ScoreDb.AddAsync(score);
        await _fixture.Context.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetScoreByIdAsync(score.Id);

        // Assert
        Assert.Equal(score.Id, result.Id);
        Assert.Equal(3, result.CompetencyScore);
    }

    [Fact]
    public async Task GetScoreByIdAsync_ShouldThrow_WhenNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ScoreNotFoundException>(() =>
            _repository.GetScoreByIdAsync(Guid.NewGuid()));
    }

    // ===== UpdateScoreAsync =====

    [Fact]
    public async Task UpdateScoreAsync_ShouldUpdateFields_WhenValid()
    {
        // Arrange
        var score = new ScoreDb(Guid.NewGuid(), _employeeId, _authorId, _positionId,
            DateTimeOffset.Now.AddHours(-3).ToUniversalTime(), 1, 2, 3);
        await _fixture.Context.ScoreDb.AddAsync(score);
        await _fixture.Context.SaveChangesAsync();

        var update = new UpdateScore(score.Id, null, 5, null, 4);

        // Act
        var result = await _repository.UpdateScoreAsync(update);

        // Assert
        Assert.Equal(5, result.EfficiencyScore);
        Assert.Equal(4, result.CompetencyScore);
        Assert.Equal(2, result.EngagementScore); // не обновлялось
    }

    // ===== DeleteScoreAsync =====

    [Fact]
    public async Task DeleteScoreAsync_ShouldRemoveScore_WhenExists()
    {
        // Arrange
        var score = new ScoreDb(Guid.NewGuid(), _employeeId, _authorId, _positionId,
            DateTimeOffset.Now.AddDays(-10).ToUniversalTime(), 1, 2, 3);
        await _fixture.Context.ScoreDb.AddAsync(score);
        await _fixture.Context.SaveChangesAsync();

        // Act
        await _repository.DeleteScoreAsync(score.Id);

        // Assert
        await Assert.ThrowsAsync<ScoreNotFoundException>(() =>
            _repository.GetScoreByIdAsync(score.Id));
    }

    // ===== GetScoresAsync =====

    [Fact]
    public async Task GetScoresAsync_ShouldReturnAllScores()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            var score = new ScoreDb(Guid.NewGuid(), _employeeId, _authorId, _positionId,
                DateTimeOffset.Now.AddDays(-i).ToUniversalTime(), 1, 2, 3);
            await _fixture.Context.ScoreDb.AddAsync(score);
        }
        await _fixture.Context.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetScoresAsync(null, null);

        // Assert
        var items = result.ToList();
        Assert.Equal(5, items.Count);
    }

    [Fact]
    public async Task GetScoresAsync_ShouldFilterByDateRange()
    {
        // Arrange
        var score1 = new ScoreDb(Guid.NewGuid(), _employeeId, _authorId, _positionId,
            DateTimeOffset.Now.AddDays(-1).ToUniversalTime(), 1, 2, 3);
        await _fixture.Context.ScoreDb.AddAsync(score1);
        var score2 = new ScoreDb(Guid.NewGuid(), _employeeId2, _employeeId, _lowestPositionId,
            DateTimeOffset.Now.AddDays(-40).ToUniversalTime(), 1, 2, 3);
        await _fixture.Context.ScoreDb.AddAsync(score2);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetScoresAsync(
            DateTimeOffset.UtcNow.AddDays(-3), DateTimeOffset.UtcNow);

        // Assert
        var items = result.ToList();
        Assert.Single(items);
        Assert.Equal(score1.Id, items[0].Id);
    }

    // ===== GetScoresByEmployeeIdAsync =====

    [Fact]
    public async Task GetScoresByEmployeeIdAsync_ShouldFilterByEmployee()
    {
        // Arrange
        var score1 = new ScoreDb(Guid.NewGuid(), _employeeId, _authorId, _positionId,
            DateTimeOffset.Now.AddDays(-10).ToUniversalTime(), 1, 2, 3);
        await _fixture.Context.ScoreDb.AddAsync(score1);
        var score2 = new ScoreDb(Guid.NewGuid(), _employeeId2, _employeeId, _lowestPositionId,
            DateTimeOffset.Now.AddDays(-10).ToUniversalTime(), 1, 2, 3);
        await _fixture.Context.ScoreDb.AddAsync(score2);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetScoresByEmployeeIdAsync(
            _employeeId, null, null, 1, 10);

        // Assert
        var items = result.ToList();
        Assert.Single(items);
        Assert.Equal(score1.Id, items[0].Id);
    }

    // ===== GetScoresByPositionIdAsync =====

    [Fact]
    public async Task GetScoresByPositionIdAsync_ShouldFilterByPosition()
    {
        // Arrange
        var score1 = new ScoreDb(Guid.NewGuid(), _employeeId, _authorId, _positionId,
            DateTimeOffset.Now.AddDays(-10).ToUniversalTime(), 1, 2, 3);
        await _fixture.Context.ScoreDb.AddAsync(score1);
        var score2 = new ScoreDb(Guid.NewGuid(), _employeeId2, _employeeId, _lowestPositionId,
            DateTimeOffset.Now.AddDays(-10).ToUniversalTime(), 1, 2, 3);
        await _fixture.Context.ScoreDb.AddAsync(score2);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetScoresByPositionIdAsync(
            _positionId, null, null, 1, 10);

        // Assert
        var items = result.ToList();
        Assert.Single(items);
        Assert.Equal(score1.Id, items[0].Id);
    }

    // ===== GetScoresByAuthorIdAsync =====

    [Fact]
    public async Task GetScoresByAuthorIdAsync_ShouldFilterByAuthor()
    {
        // Arrange
        var score1 = new ScoreDb(Guid.NewGuid(), _employeeId, _authorId, _positionId,
            DateTimeOffset.Now.AddDays(-10).ToUniversalTime(), 1, 2, 3);
        await _fixture.Context.ScoreDb.AddAsync(score1);
        var score2 = new ScoreDb(Guid.NewGuid(), _employeeId2, _employeeId, _lowestPositionId,
            DateTimeOffset.Now.AddDays(-10).ToUniversalTime(), 1, 2, 3);
        await _fixture.Context.ScoreDb.AddAsync(score2);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetScoresByAuthorIdAsync(
            _authorId, null, null, 1, 10);

        // Assert
        var items = result.ToList();
        Assert.Single(items);
        Assert.Equal(score1.Id, items[0].Id);
    }

    // ===== GetSubordinatesLastScoresByEmployeeIdAsync =====

    [Fact]
    public async Task GetSubordinatesLastScoresByEmployeeIdAsync_ShouldReturnSubordinatesScores()
    {
        // Arrange
        var random = new Random(42); // фиксированный seed для повторяемости
        
        for (int i = 0; i < 3; i++)
        {
            var score0 = new ScoreDb(Guid.NewGuid(), _authorId, _directorId, _managerPositionId,
                DateTimeOffset.Now.AddDays(-i * 30).ToUniversalTime(),
                random.Next(1, 6), random.Next(1, 6), random.Next(1, 6));
            var score1 = new ScoreDb(Guid.NewGuid(), _employeeId, _authorId, _positionId,
                DateTimeOffset.Now.AddDays(-i * 30).ToUniversalTime(),
                random.Next(1, 6), random.Next(1, 6), random.Next(1, 6));
            var score2 = new ScoreDb(Guid.NewGuid(), _employeeId2, _employeeId, _lowestPositionId,
                DateTimeOffset.Now.AddDays(-i * 30).ToUniversalTime(),
                random.Next(1, 6), random.Next(1, 6), random.Next(1, 6));
            await _fixture.Context.ScoreDb.AddRangeAsync(score0, score1, score2);
        }
        await _fixture.Context.SaveChangesAsync();
    
        // Act
        var result = await _repository.GetSubordinatesLastScoresByEmployeeIdAsync(_employeeId);
    
        // Assert — employeeId is Developer, subordinate is employeeId2 (Junior).
        // GetSubordinatesLastScoresByEmployeeIdAsync should return last scores
        // for each subordinate (employees under employeeId's position).
        var items = result.ToList();
        Assert.NotEmpty(items);
        Assert.DoesNotContain(_authorId, items.Select(x => x.EmployeeId));
    }
}
