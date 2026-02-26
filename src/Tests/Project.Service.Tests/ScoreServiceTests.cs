using Database.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Core.Exceptions;
using Project.Service.Tests.Factories;
using Project.Service.Tests.Fixtures;
using Project.Services.ScoreService;
using Xunit;

namespace Project.Service.Tests;

[Collection("Database")]
public class ScoreServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly ScoreService _scoreService;
    private Guid _companyId;
    private Guid _employeeId;
    private Guid _authorId;
    private Guid _positionId;

    public ScoreServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;

        var scoreRepository = new ScoreRepository(
            _fixture.Context,
            NullLogger<ScoreRepository>.Instance);

        _scoreService = new ScoreService(
            scoreRepository,
            NullLogger<ScoreService>.Instance);
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

        // Создаём компанию
        var companyRepo = new CompanyRepository(
            _fixture.Context,
            NullLogger<CompanyRepository>.Instance);
        var company = CompanyObjectFabric.CreateValidCreationCompany();
        var createdCompany = await companyRepo.AddCompanyAsync(company);
        _companyId = createdCompany.CompanyId;

        // Создаём сотрудника (объект оценки)
        var employeeRepo = new EmployeeRepository(
            _fixture.Context,
            NullLogger<EmployeeRepository>.Instance);
        var employee = EmployeeObjectFabric.CreateValidCreationEmployee();
        var createdEmployee = await employeeRepo.AddEmployeeAsync(employee);
        _employeeId = createdEmployee.EmployeeId;

        // Создаём автора оценки (другой сотрудник)
        var author = EmployeeObjectFabric.CreateValidCreationEmployee("Петр Автор");
        var createdAuthor = await employeeRepo.AddEmployeeAsync(author);
        _authorId = createdAuthor.EmployeeId;

        // Создаём позицию
        var positionRepo = new PositionRepository(
            _fixture.Context,
            NullLogger<PositionRepository>.Instance);
        var position = PositionObjectFabric.CreateValidCreatePosition(_companyId);
        var createdPosition = await positionRepo.AddPositionAsync(position);
        _positionId = createdPosition.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ===== AddScoreAsync =====

    [Fact]
    public async Task AddScoreAsync_ValidData_ReturnsCreatedScore()
    {
        // Arrange
        var creation = ScoreObjectFabric.CreateValidCreateScore(_employeeId, _authorId, _positionId);

        // Act
        var result = await _scoreService.AddScoreAsync(
            _employeeId, _authorId, _positionId,
            creation.CreatedAt, creation.EfficiencyScore,
            creation.EngagementScore, creation.CompetencyScore);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(_employeeId, result.EmployeeId);
        Assert.Equal(_authorId, result.AuthorId);
        Assert.Equal(_positionId, result.PositionId);
        Assert.Equal(creation.EfficiencyScore, result.EfficiencyScore);
        Assert.Equal(creation.EngagementScore, result.EngagementScore);
        Assert.Equal(creation.CompetencyScore, result.CompetencyScore);
    }

    // ===== GetScoreAsync =====

    [Fact]
    public async Task GetScoreAsync_Existing_ReturnsScore()
    {
        // Arrange
        var creation = ScoreObjectFabric.CreateValidCreateScore(_employeeId, _authorId, _positionId);
        var created = await _scoreService.AddScoreAsync(
            _employeeId, _authorId, _positionId,
            creation.CreatedAt, creation.EfficiencyScore,
            creation.EngagementScore, creation.CompetencyScore);

        // Act
        var result = await _scoreService.GetScoreAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal(_employeeId, result.EmployeeId);
    }

    [Fact]
    public async Task GetScoreAsync_NonExistent_ThrowsScoreNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ScoreNotFoundException>(() =>
            _scoreService.GetScoreAsync(nonExistentId));
    }

    // ===== UpdateScoreAsync =====

    [Fact]
    public async Task UpdateScoreAsync_ValidData_ReturnsUpdatedScore()
    {
        // Arrange
        var creation = ScoreObjectFabric.CreateValidCreateScore(_employeeId, _authorId, _positionId);
        var created = await _scoreService.AddScoreAsync(
            _employeeId, _authorId, _positionId,
            creation.CreatedAt, creation.EfficiencyScore,
            creation.EngagementScore, creation.CompetencyScore);

        // Act
        var result = await _scoreService.UpdateScoreAsync(created.Id, null, 5, 5, 5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.EfficiencyScore);
        Assert.Equal(5, result.EngagementScore);
        Assert.Equal(5, result.CompetencyScore);
    }

    [Fact]
    public async Task UpdateScoreAsync_NonExistent_ThrowsScoreNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ScoreNotFoundException>(() =>
            _scoreService.UpdateScoreAsync(nonExistentId, null, 5, null, null));
    }

    // ===== GetScoresByEmployeeIdAsync =====

    [Fact]
    public async Task GetScoresByEmployeeIdAsync_WithData_ReturnsScores()
    {
        // Arrange
        var creation = ScoreObjectFabric.CreateValidCreateScore(_employeeId, _authorId, _positionId);
        await _scoreService.AddScoreAsync(
            _employeeId, _authorId, _positionId,
            creation.CreatedAt, creation.EfficiencyScore,
            creation.EngagementScore, creation.CompetencyScore);

        // Act
        var result = await _scoreService.GetScoresByEmployeeIdAsync(
            _employeeId, null, null, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetScoresByEmployeeIdAsync_NoData_ReturnsEmptyList()
    {
        // Arrange
        var unknownEmployeeId = Guid.NewGuid();

        // Act
        var result = await _scoreService.GetScoresByEmployeeIdAsync(
            unknownEmployeeId, null, null, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // ===== GetScoresByAuthorIdAsync =====

    [Fact]
    public async Task GetScoresByAuthorIdAsync_WithData_ReturnsScores()
    {
        // Arrange
        var creation = ScoreObjectFabric.CreateValidCreateScore(_employeeId, _authorId, _positionId);
        await _scoreService.AddScoreAsync(
            _employeeId, _authorId, _positionId,
            creation.CreatedAt, creation.EfficiencyScore,
            creation.EngagementScore, creation.CompetencyScore);

        // Act
        var result = await _scoreService.GetScoresByAuthorIdAsync(
            _authorId, null, null, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    // ===== GetScoresByPositionIdAsync =====

    [Fact]
    public async Task GetScoresByPositionIdAsync_WithData_ReturnsScores()
    {
        // Arrange
        var creation = ScoreObjectFabric.CreateValidCreateScore(_employeeId, _authorId, _positionId);
        await _scoreService.AddScoreAsync(
            _employeeId, _authorId, _positionId,
            creation.CreatedAt, creation.EfficiencyScore,
            creation.EngagementScore, creation.CompetencyScore);

        // Act
        var result = await _scoreService.GetScoresByPositionIdAsync(
            _positionId, null, null, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    // ===== DeleteScoreAsync =====

    [Fact]
    public async Task DeleteScoreAsync_Existing_DeletesSuccessfully()
    {
        // Arrange
        var creation = ScoreObjectFabric.CreateValidCreateScore(_employeeId, _authorId, _positionId);
        var created = await _scoreService.AddScoreAsync(
            _employeeId, _authorId, _positionId,
            creation.CreatedAt, creation.EfficiencyScore,
            creation.EngagementScore, creation.CompetencyScore);

        // Act
        await _scoreService.DeleteScoreAsync(created.Id);

        // Assert
        await Assert.ThrowsAsync<ScoreNotFoundException>(() =>
            _scoreService.GetScoreAsync(created.Id));
    }

    [Fact]
    public async Task DeleteScoreAsync_NonExistent_ThrowsScoreNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ScoreNotFoundException>(() =>
            _scoreService.DeleteScoreAsync(nonExistentId));
    }
}
