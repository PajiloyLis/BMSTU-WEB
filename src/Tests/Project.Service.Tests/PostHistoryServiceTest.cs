using Database.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Core.Exceptions;
using Project.Service.Tests.Factories;
using Project.Service.Tests.Fixtures;
using Project.Services.PostHistoryService;
using Xunit;

namespace Project.Service.Tests;

[Collection("Database")]
public class PostHistoryServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly PostHistoryService _postHistoryService;
    private Guid _companyId;
    private Guid _employeeId;
    private Guid _postId;

    public PostHistoryServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;

        var postHistoryRepository = new PostHistoryRepository(
            _fixture.Context,
            NullLogger<PostHistoryRepository>.Instance);

        _postHistoryService = new PostHistoryService(
            postHistoryRepository,
            NullLogger<PostHistoryService>.Instance,
            null!); // IConnectionMultiplexer не используется
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

        // Создаём должность (Post)
        var postRepo = new PostRepository(
            _fixture.Context,
            NullLogger<PostRepository>.Instance);
        var post = PostObjectFabric.CreateValidCreatePost(_companyId);
        var createdPost = await postRepo.AddPostAsync(post);
        _postId = createdPost.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ===== AddPostHistoryAsync =====

    [Fact]
    public async Task AddPostHistoryAsync_ValidData_ReturnsCreatedPostHistory()
    {
        // Arrange
        var creation = PostHistoryObjectFabric.CreateValidCreatePostHistory(_postId, _employeeId);

        // Act
        var result = await _postHistoryService.AddPostHistoryAsync(
            _postId, _employeeId, creation.StartDate, creation.EndDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_postId, result.PostId);
        Assert.Equal(_employeeId, result.EmployeeId);
        Assert.Equal(creation.StartDate, result.StartDate);
    }

    // ===== GetPostHistoryAsync =====

    [Fact]
    public async Task GetPostHistoryAsync_Existing_ReturnsPostHistory()
    {
        // Arrange
        var creation = PostHistoryObjectFabric.CreateValidCreatePostHistory(_postId, _employeeId);
        await _postHistoryService.AddPostHistoryAsync(
            _postId, _employeeId, creation.StartDate, creation.EndDate);

        // Act
        var result = await _postHistoryService.GetPostHistoryAsync(_postId, _employeeId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_postId, result.PostId);
        Assert.Equal(_employeeId, result.EmployeeId);
    }

    [Fact]
    public async Task GetPostHistoryAsync_NonExistent_ThrowsPostHistoryNotFoundException()
    {
        // Arrange
        var nonExistentPostId = Guid.NewGuid();
        var nonExistentEmployeeId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<PostHistoryNotFoundException>(() =>
            _postHistoryService.GetPostHistoryAsync(nonExistentPostId, nonExistentEmployeeId));
    }

    // ===== UpdatePostHistoryAsync =====

    [Fact]
    public async Task UpdatePostHistoryAsync_ValidData_ReturnsUpdatedPostHistory()
    {
        // Arrange
        var creation = PostHistoryObjectFabric.CreateValidCreatePostHistory(_postId, _employeeId);
        await _postHistoryService.AddPostHistoryAsync(
            _postId, _employeeId, creation.StartDate, creation.EndDate);

        var newStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20));

        // Act
        var result = await _postHistoryService.UpdatePostHistoryAsync(
            _postId, _employeeId, newStartDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newStartDate, result.StartDate);
    }

    [Fact]
    public async Task UpdatePostHistoryAsync_NonExistent_ThrowsPostHistoryNotFoundException()
    {
        // Arrange
        var nonExistentPostId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<PostHistoryNotFoundException>(() =>
            _postHistoryService.UpdatePostHistoryAsync(nonExistentPostId, _employeeId,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30))));
    }

    // ===== DeletePostHistoryAsync =====

    [Fact]
    public async Task DeletePostHistoryAsync_Existing_DeletesSuccessfully()
    {
        // Arrange
        var creation = PostHistoryObjectFabric.CreateValidCreatePostHistory(_postId, _employeeId);
        await _postHistoryService.AddPostHistoryAsync(
            _postId, _employeeId, creation.StartDate, creation.EndDate);

        // Act
        await _postHistoryService.DeletePostHistoryAsync(_postId, _employeeId);

        // Assert
        await Assert.ThrowsAsync<PostHistoryNotFoundException>(() =>
            _postHistoryService.GetPostHistoryAsync(_postId, _employeeId));
    }

    [Fact]
    public async Task DeletePostHistoryAsync_NonExistent_ThrowsPostHistoryNotFoundException()
    {
        // Arrange
        var nonExistentPostId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<PostHistoryNotFoundException>(() =>
            _postHistoryService.DeletePostHistoryAsync(nonExistentPostId, _employeeId));
    }

    // ===== GetPostHistoryByEmployeeIdAsync =====

    [Fact]
    public async Task GetPostHistoryByEmployeeIdAsync_WithData_ReturnsRecords()
    {
        // Arrange
        var creation = PostHistoryObjectFabric.CreateCompletedPostHistory(_postId, _employeeId);
        await _postHistoryService.AddPostHistoryAsync(
            _postId, _employeeId, creation.StartDate, creation.EndDate);

        // Act
        var result = await _postHistoryService.GetPostHistoryByEmployeeIdAsync(
            _employeeId, null, null, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetPostHistoryByEmployeeIdAsync_NoData_ReturnsEmptyList()
    {
        // Arrange
        var unknownEmployeeId = Guid.NewGuid();

        // Act
        var result = await _postHistoryService.GetPostHistoryByEmployeeIdAsync(
            unknownEmployeeId, null, null, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}