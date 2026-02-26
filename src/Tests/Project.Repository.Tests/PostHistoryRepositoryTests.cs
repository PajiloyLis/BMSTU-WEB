using Database.Models;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Core.Exceptions;
using Project.Core.Models.PostHistory;
using Project.Database.Models;
using Project.Repository.Tests.Factories;
using Project.Repository.Tests.Fixtures;
using Xunit;

namespace Project.Repository.Tests;

[Collection("Database")]
public class PostHistoryRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly PostHistoryRepository _repository;

    private Guid _companyId;
    private Guid _employeeId1;
    private Guid _employeeId2;
    private Guid _employeeId3;
    private Guid _employeeId4;
    private Guid _postId1;
    private Guid _postId2;
    private Guid _postId3;
    private Guid _postId4;
    private Guid _positionId1;
    private Guid _positionId2;
    private Guid _positionId3;
    private Guid _positionId4;

    public PostHistoryRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new PostHistoryRepository(
            _fixture.Context,
            NullLogger<PostHistoryRepository>.Instance);
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

        _companyId = Guid.NewGuid();
        _employeeId1 = Guid.NewGuid();
        _employeeId2 = Guid.NewGuid();
        _employeeId3 = Guid.NewGuid();
        _employeeId4 = Guid.NewGuid();
        _postId1 = Guid.NewGuid();
        _postId2 = Guid.NewGuid();
        _postId3 = Guid.NewGuid();
        _postId4 = Guid.NewGuid();
        _positionId1 = Guid.NewGuid();
        _positionId2 = Guid.NewGuid();
        _positionId3 = Guid.NewGuid();
        _positionId4 = Guid.NewGuid();

        var company = new CompanyDb(_companyId,
            "Test Company PH",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            "+1234567890", "ph_test@example.com",
            "1234567890", "123456789", "1234567890123", "Test Address");
        await _fixture.Context.CompanyDb.AddAsync(company);

        var employees = new List<EmployeeDb>
        {
            new(_employeeId1, "John Doe", "+1234567890", "john.doe@example.com",
                new DateOnly(1990, 1, 1), "johnphoto.jpg", "{\"Developer\": true}"),
            new(_employeeId2, "Jason Doe", "+1234567899", "jason.doe@example.com",
                new DateOnly(1991, 1, 1), "jasonphoto.jpg", "{\"Developer\": true}"),
            new(_employeeId3, "Jack Doe", "+1234467890", "jack.doe@example.com",
                new DateOnly(1989, 1, 1), "jackphoto.jpg", "{\"HR Manager\": true}"),
            new(_employeeId4, "George Doe", "+91234567890", "george.doe@example.com",
                new DateOnly(1995, 2, 1), "georgephoto.jpg", null)
        };
        await _fixture.Context.EmployeeDb.AddRangeAsync(employees);

        var posts = new List<PostDb>
        {
            new(_postId1, "Manager", 200000, _companyId),
            new(_postId2, "Senior Developer", 195000, _companyId),
            new(_postId3, "Middle Developer", 150000, _companyId),
            new(_postId4, "Junior Developer", 100000, _companyId)
        };
        await _fixture.Context.PostDb.AddRangeAsync(posts);

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

    // ===== AddPostHistoryAsync =====
    
    [Fact]
    public async Task AddPostHistoryAsync_ShouldAddNewPostHistory()
    {
        // Arrange
        var createDto = new CreatePostHistory(
            _postId1, _employeeId1,
            DateOnly.FromDateTime(DateTime.Today).AddDays(-1), null);

        // Act
        var result = await _repository.AddPostHistoryAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_postId1, result.PostId);
        Assert.Equal(_employeeId1, result.EmployeeId);
        Assert.Equal(createDto.StartDate, result.StartDate);
        Assert.Null(result.EndDate);
        
        var dbEntry = await _fixture.Context.PostHistoryDb.FirstOrDefaultAsync();
        Assert.NotNull(dbEntry);
    }

    // ===== GetPostHistoryByIdAsync =====

    [Fact]
    public async Task GetPostHistoryByIdAsync_ShouldReturnPostHistory_WhenExists()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);
        var entity = new PostHistoryDb(_postId1, _employeeId1, startDate);
        await _fixture.Context.PostHistoryDb.AddAsync(entity);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPostHistoryByIdAsync(_postId1, _employeeId1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_postId1, result.PostId);
        Assert.Equal(_employeeId1, result.EmployeeId);
        Assert.Equal(startDate, result.StartDate);
    }

    [Fact]
    public async Task GetPostHistoryByIdAsync_ShouldThrow_WhenNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<PostHistoryNotFoundException>(() => 
            _repository.GetPostHistoryByIdAsync(_postId1, _employeeId1));
    }

    // ===== UpdatePostHistoryAsync =====

    [Fact]
    public async Task UpdatePostHistoryAsync_ShouldUpdateDates_WhenExists()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-10);
        var newEndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        
        var entity = new PostHistoryDb(_postId1, _employeeId1, startDate);
        await _fixture.Context.PostHistoryDb.AddAsync(entity);
        await _fixture.Context.SaveChangesAsync();

        var updateDto = new UpdatePostHistory(_postId1, _employeeId1, null, newEndDate);

        // Act
        var result = await _repository.UpdatePostHistoryAsync(updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newEndDate, result.EndDate);
        Assert.Equal(startDate, result.StartDate);
    }

    // ===== DeletePostHistoryAsync =====

    [Fact]
    public async Task DeletePostHistoryAsync_ShouldRemoveEntry_WhenExists()
    {
        // Arrange
        var entity = new PostHistoryDb(
            _postId1, _employeeId1,
            DateOnly.FromDateTime(DateTime.Today).AddDays(-1));
        await _fixture.Context.PostHistoryDb.AddAsync(entity);
        await _fixture.Context.SaveChangesAsync();

        // Act
        await _repository.DeletePostHistoryAsync(_postId1, _employeeId1);

        // Assert
        var dbEntry = await _fixture.Context.PostHistoryDb.FirstOrDefaultAsync(
            ph => ph.PostId == _postId1 && ph.EmployeeId == _employeeId1);
        Assert.Null(dbEntry);
    }

    // ===== GetPostHistoryByEmployeeIdAsync =====

    [Fact]
    public async Task GetPostHistoryByEmployeeIdAsync_ShouldReturnPagedResults()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-50));
        var histories = new List<PostHistoryDb>
        {
            new(_postId1, _employeeId1, startDate, startDate.AddDays(10)),
            new(_postId2, _employeeId1, startDate.AddDays(11), startDate.AddDays(20)),
            new(_postId3, _employeeId1, startDate.AddDays(21))
        };
        await _fixture.Context.PostHistoryDb.AddRangeAsync(histories);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var page1 = await _repository.GetPostHistoryByEmployeeIdAsync(
            _employeeId1, startDate: null, endDate: null, pageNumber: 1, pageSize: 2);

        // Assert
        var items = page1.ToList();
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task GetPostHistoryByEmployeeIdAsync_ShouldFilterByDateRange()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-50));
        var filterStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-35));
        
        var histories = new List<PostHistoryDb>
        {
            new(_postId1, _employeeId1, startDate, startDate.AddDays(10)),
            new(_postId2, _employeeId1, startDate.AddDays(11), startDate.AddDays(20)),
            new(_postId3, _employeeId1, startDate.AddDays(21))
        };
        await _fixture.Context.PostHistoryDb.AddRangeAsync(histories);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPostHistoryByEmployeeIdAsync(
            _employeeId1, startDate: filterStartDate, endDate: null,
            pageNumber: 1, pageSize: 10);

        // Assert
        var items = result.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.PostId == _postId2);
        Assert.Contains(items, x => x.PostId == _postId3);
    }

    // ===== GetSubordinatesPostHistoryAsync =====

    [Fact]
    public async Task GetSubordinatesPostHistoryAsync_ShouldReturnSubordinatesHistory()
    {
        // Arrange — создаём иерархию позиций и историю
        var hierarchy = new List<PostHistoryDb>
        {
            new(_postId1, _employeeId1, new DateOnly(2015, 1, 1)),
            new(_postId2, _employeeId2, new DateOnly(2015, 1, 1), new DateOnly(2020, 1, 1)),
            new(_postId3, _employeeId3, new DateOnly(2015, 1, 1), new DateOnly(2020, 1, 1)),
            new(_postId4, _employeeId4, new DateOnly(2015, 1, 1), new DateOnly(2020, 1, 1)),
            new(_postId2, _employeeId4, new DateOnly(2020, 1, 1), new DateOnly(2025, 1, 1)),
            new(_postId3, _employeeId2, new DateOnly(2020, 1, 1), new DateOnly(2025, 1, 1)),
            new(_postId4, _employeeId3, new DateOnly(2020, 1, 1), new DateOnly(2025, 1, 1)),
            new(_postId2, _employeeId3, new DateOnly(2025, 1, 1)),
            new(_postId3, _employeeId4, new DateOnly(2025, 1, 1)),
            new(_postId4, _employeeId2, new DateOnly(2025, 1, 1))
        };
        await _fixture.Context.PostHistoryDb.AddRangeAsync(hierarchy);

        var positionHistory = new List<PositionHistoryDb>
        {
            new(_positionId1, _employeeId1, new DateOnly(2015, 1, 1)),
            new(_positionId2, _employeeId3, new DateOnly(2025, 1, 1)),
            new(_positionId3, _employeeId4, new DateOnly(2025, 1, 1)),
            new(_positionId4, _employeeId2, new DateOnly(2025, 1, 1))
        };
        await _fixture.Context.PositionHistoryDb.AddRangeAsync(positionHistory);
        await _fixture.Context.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetSubordinatesPostHistoryAsync(
            _employeeId1, startDate: null, endDate: null,
            pageNumber: 1, pageSize: 3);

        // Assert
        var items = result.ToList();
        Assert.Equal(3, items.Count);
    }
}
