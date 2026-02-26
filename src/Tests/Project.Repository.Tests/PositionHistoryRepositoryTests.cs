using Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Core.Exceptions;
using Project.Core.Models.PositionHistory;
using Project.Database.Models;
using Project.Database.Repositories;
using Project.Repository.Tests.Factories;
using Project.Repository.Tests.Fixtures;
using Xunit;

namespace Project.Repository.Tests;

[Collection("Database")]
public class PositionHistoryRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly PositionHistoryRepository _repository;

    private Guid _companyId;
    private Guid _employeeId1;
    private Guid _employeeId2;
    private Guid _employeeId3;
    private Guid _employeeId4;
    private Guid _positionId1;
    private Guid _positionId2;
    private Guid _positionId3;
    private Guid _positionId4;

    public PositionHistoryRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new PositionHistoryRepository(
            _fixture.Context,
            NullLogger<PositionHistoryRepository>.Instance);
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

        _companyId = Guid.NewGuid();
        _employeeId1 = Guid.NewGuid();
        _employeeId2 = Guid.NewGuid();
        _employeeId3 = Guid.NewGuid();
        _employeeId4 = Guid.NewGuid();
        _positionId1 = Guid.NewGuid();
        _positionId2 = Guid.NewGuid();
        _positionId3 = Guid.NewGuid();
        _positionId4 = Guid.NewGuid();

        var company = new CompanyDb(_companyId,
            "PosHist Test Company",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            "+2234567890", "poshist@example.com",
            "2234567890", "223456789", "2234567890123", "PH Address");
        await _fixture.Context.CompanyDb.AddAsync(company);

        var employees = new List<EmployeeDb>
        {
            new(_employeeId1, "John Doe PH", "+2234567890", "john.ph@example.com",
                new DateOnly(1990, 1, 1), "johnphoto.jpg", "{\"Developer\": true}"),
            new(_employeeId2, "Jason Doe PH", "+2234567899", "jason.ph@example.com",
                new DateOnly(1991, 1, 1), "jasonphoto.jpg", "{\"Developer\": true}"),
            new(_employeeId3, "Jack Doe PH", "+2234467890", "jack.ph@example.com",
                new DateOnly(1989, 1, 1), "jackphoto.jpg", "{\"HR Manager\": true}"),
            new(_employeeId4, "George Doe PH", "+92234567890", "george.ph@example.com",
                new DateOnly(1995, 2, 1), "georgephoto.jpg", null)
        };
        await _fixture.Context.EmployeeDb.AddRangeAsync(employees);

        var positions = new List<PositionDb>
        {
            new(_positionId1, null, "Manager", _companyId),
            new(_positionId2, _positionId1, "Senior Developer", _companyId),
            new(_positionId3, _positionId2, "Middle Developer", _companyId),
            new(_positionId4, _positionId3, "Junior Developer", _companyId)
        };
        await _fixture.Context.PositionDb.AddRangeAsync(positions);

        await _fixture.Context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ===== AddPositionHistoryAsync =====

    [Fact]
    public async Task AddPositionHistoryAsync_ShouldAddAndReturnPositionHistory()
    {
        // Arrange
        var createDto = new CreatePositionHistory(
            _positionId1, _employeeId1,
            DateOnly.FromDateTime(DateTime.Today).AddDays(-1));

        // Act
        var result = await _repository.AddPositionHistoryAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_employeeId1, result.EmployeeId);
        Assert.Equal(_positionId1, result.PositionId);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today).AddDays(-1), result.StartDate);

        var dbEntry = await _fixture.Context.PositionHistoryDb.FirstOrDefaultAsync();
        Assert.NotNull(dbEntry);
        Assert.Equal(_positionId1, dbEntry!.PositionId);
    }

    // ===== GetPositionHistoryByIdAsync =====

    [Fact]
    public async Task GetPositionHistoryByIdAsync_ShouldReturnPositionHistory_WhenExists()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);
        var entity = new PositionHistoryDb(_positionId1, _employeeId1, startDate);
        await _fixture.Context.PositionHistoryDb.AddAsync(entity);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPositionHistoryByIdAsync(_positionId1, _employeeId1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_positionId1, result.PositionId);
        Assert.Equal(_employeeId1, result.EmployeeId);
        Assert.Equal(startDate, result.StartDate);
    }

    [Fact]
    public async Task GetPositionHistoryByIdAsync_ShouldThrow_WhenNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<PositionHistoryNotFoundException>(() =>
            _repository.GetPositionHistoryByIdAsync(_positionId1, _employeeId1));
    }

    // ===== UpdatePositionHistoryAsync =====

    [Fact]
    public async Task UpdatePositionHistoryAsync_ShouldUpdateFields_WhenExists()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-5);
        var newEndDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);

        var entity = new PositionHistoryDb(_positionId1, _employeeId1, startDate);
        await _fixture.Context.PositionHistoryDb.AddAsync(entity);
        await _fixture.Context.SaveChangesAsync();

        var updateDto = new UpdatePositionHistory(
            _positionId1, _employeeId1, startDate, newEndDate);

        // Act
        var result = await _repository.UpdatePositionHistoryAsync(updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newEndDate, result.EndDate);
    }

    // ===== DeletePositionHistoryAsync =====

    [Fact]
    public async Task DeletePositionHistoryAsync_ShouldRemoveEntry_WhenExists()
    {
        // Arrange
        var entity = new PositionHistoryDb(
            _positionId1, _employeeId1,
            DateOnly.FromDateTime(DateTime.Today).AddDays(-1));
        await _fixture.Context.PositionHistoryDb.AddAsync(entity);
        await _fixture.Context.SaveChangesAsync();

        // Act
        await _repository.DeletePositionHistoryAsync(_positionId1, _employeeId1);

        // Assert
        var dbEntry = await _fixture.Context.PositionHistoryDb.FirstOrDefaultAsync(
            ph => ph.PositionId == _positionId1 && ph.EmployeeId == _employeeId1);
        Assert.Null(dbEntry);
    }

    // ===== GetPositionHistoryByEmployeeIdAsync =====

    [Fact]
    public async Task GetPositionHistoryByEmployeeIdAsync_ShouldReturnResults()
    {
        // Arrange
        var history = new List<PositionHistoryDb>
        {
            new(_positionId4, _employeeId1, new DateOnly(2018, 1, 1), new DateOnly(2020, 1, 1)),
            new(_positionId3, _employeeId1, new DateOnly(2020, 1, 1), new DateOnly(2022, 1, 1)),
            new(_positionId2, _employeeId1, new DateOnly(2022, 1, 1), new DateOnly(2024, 1, 1)),
            new(_positionId1, _employeeId1, new DateOnly(2024, 1, 1))
        };
        await _fixture.Context.PositionHistoryDb.AddRangeAsync(history);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPositionHistoryByEmployeeIdAsync(
            _employeeId1, DateOnly.MinValue, null);

        // Assert
        var items = result.ToList();
        Assert.Equal(4, items.Count);
    }

    // ===== GetCurrentSubordinatesAsync =====

    [Fact]
    public async Task GetCurrentSubordinatesAsync_ShouldReturnSubordinates()
    {
        // Arrange
        var hierarchy = new List<PositionHistoryDb>
        {
            new(_positionId1, _employeeId1, new DateOnly(2015, 1, 1)),
            new(_positionId2, _employeeId3, new DateOnly(2025, 1, 1)),
            new(_positionId3, _employeeId4, new DateOnly(2025, 1, 1)),
            new(_positionId4, _employeeId2, new DateOnly(2025, 1, 1))
        };
        await _fixture.Context.PositionHistoryDb.AddRangeAsync(hierarchy);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetCurrentSubordinatesAsync(
            _employeeId1, 1, 10);

        // Assert
        var items = result.ToList();
        Assert.Equal(4, items.Count); // manager + 3 subordinates
    }
}
