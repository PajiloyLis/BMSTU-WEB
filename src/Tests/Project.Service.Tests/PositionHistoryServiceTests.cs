using Database.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Core.Exceptions;
using Project.Database.Repositories;
using Project.Service.Tests.Factories;
using Project.Service.Tests.Fixtures;
using Project.Services.PositionHistoryService;
using Xunit;

namespace Project.Service.Tests;

[Collection("Database")]
public class PositionHistoryServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly PositionHistoryService _positionHistoryService;
    private Guid _companyId;
    private Guid _employeeId;
    private Guid _positionId;

    public PositionHistoryServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;

        var positionHistoryRepository = new PositionHistoryRepository(
            _fixture.Context,
            NullLogger<PositionHistoryRepository>.Instance);

        _positionHistoryService = new PositionHistoryService(
            positionHistoryRepository,
            NullLogger<PositionHistoryService>.Instance,
            null!);
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

        // Создаём сотрудника
        var employeeRepo = new EmployeeRepository(
            _fixture.Context,
            NullLogger<EmployeeRepository>.Instance);
        var employee = EmployeeObjectFabric.CreateValidCreationEmployee();
        var createdEmployee = await employeeRepo.AddEmployeeAsync(employee);
        _employeeId = createdEmployee.EmployeeId;

        // Создаём позицию
        var positionRepo = new PositionRepository(
            _fixture.Context,
            NullLogger<PositionRepository>.Instance);
        var position = PositionObjectFabric.CreateValidCreatePosition(_companyId);
        var createdPosition = await positionRepo.AddPositionAsync(position);
        _positionId = createdPosition.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ===== AddPositionHistoryAsync =====

    [Fact]
    public async Task AddPositionHistoryAsync_ValidData_ReturnsCreatedRecord()
    {
        // Arrange
        var creation = PositionHistoryObjectFabric.CreateValidCreatePositionHistory(_positionId, _employeeId);

        // Act
        var result = await _positionHistoryService.AddPositionHistoryAsync(
            _positionId, _employeeId, creation.StartDate, creation.EndDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_positionId, result.PositionId);
        Assert.Equal(_employeeId, result.EmployeeId);
        Assert.Equal(creation.StartDate, result.StartDate);
    }

    // ===== GetPositionHistoryAsync =====

    [Fact]
    public async Task GetPositionHistoryAsync_Existing_ReturnsRecord()
    {
        // Arrange
        var creation = PositionHistoryObjectFabric.CreateValidCreatePositionHistory(_positionId, _employeeId);
        await _positionHistoryService.AddPositionHistoryAsync(
            _positionId, _employeeId, creation.StartDate, creation.EndDate);

        // Act
        var result = await _positionHistoryService.GetPositionHistoryAsync(_positionId, _employeeId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_positionId, result.PositionId);
        Assert.Equal(_employeeId, result.EmployeeId);
    }

    [Fact]
    public async Task GetPositionHistoryAsync_NonExistent_ThrowsPositionHistoryNotFoundException()
    {
        // Arrange
        var nonExistentPositionId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<PositionHistoryNotFoundException>(() =>
            _positionHistoryService.GetPositionHistoryAsync(nonExistentPositionId, _employeeId));
    }

    // ===== UpdatePositionHistoryAsync =====

    [Fact]
    public async Task UpdatePositionHistoryAsync_ValidData_ReturnsUpdatedRecord()
    {
        // Arrange
        var creation = PositionHistoryObjectFabric.CreateValidCreatePositionHistory(_positionId, _employeeId);
        await _positionHistoryService.AddPositionHistoryAsync(
            _positionId, _employeeId, creation.StartDate, creation.EndDate);

        var newEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

        // Act
        var result = await _positionHistoryService.UpdatePositionHistoryAsync(
            _positionId, _employeeId, null, newEndDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newEndDate, result.EndDate);
    }

    [Fact]
    public async Task UpdatePositionHistoryAsync_NonExistent_ThrowsPositionHistoryNotFoundException()
    {
        // Arrange
        var nonExistentPositionId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<PositionHistoryNotFoundException>(() =>
            _positionHistoryService.UpdatePositionHistoryAsync(
                nonExistentPositionId, _employeeId,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10))));
    }

    // ===== DeletePositionHistoryAsync =====

    [Fact]
    public async Task DeletePositionHistoryAsync_Existing_DeletesSuccessfully()
    {
        // Arrange
        var creation = PositionHistoryObjectFabric.CreateValidCreatePositionHistory(_positionId, _employeeId);
        await _positionHistoryService.AddPositionHistoryAsync(
            _positionId, _employeeId, creation.StartDate, creation.EndDate);

        // Act
        await _positionHistoryService.DeletePositionHistoryAsync(_positionId, _employeeId);

        // Assert
        await Assert.ThrowsAsync<PositionHistoryNotFoundException>(() =>
            _positionHistoryService.GetPositionHistoryAsync(_positionId, _employeeId));
    }

    [Fact]
    public async Task DeletePositionHistoryAsync_NonExistent_ThrowsPositionHistoryNotFoundException()
    {
        // Arrange
        var nonExistentPositionId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<PositionHistoryNotFoundException>(() =>
            _positionHistoryService.DeletePositionHistoryAsync(nonExistentPositionId, _employeeId));
    }

    // ===== GetPositionHistoryByEmployeeIdAsync =====

    [Fact]
    public async Task GetPositionHistoryByEmployeeIdAsync_WithData_ReturnsRecords()
    {
        // Arrange
        var creation = PositionHistoryObjectFabric.CreateCompletedPositionHistory(_positionId, _employeeId);
        await _positionHistoryService.AddPositionHistoryAsync(
            _positionId, _employeeId, creation.StartDate, creation.EndDate);

        // Act
        var result = await _positionHistoryService.GetPositionHistoryByEmployeeIdAsync(
            _employeeId, null, null);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetPositionHistoryByEmployeeIdAsync_NoData_ReturnsEmptyList()
    {
        // Arrange
        var unknownEmployeeId = Guid.NewGuid();

        // Act
        var result = await _positionHistoryService.GetPositionHistoryByEmployeeIdAsync(
            unknownEmployeeId, null, null);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
