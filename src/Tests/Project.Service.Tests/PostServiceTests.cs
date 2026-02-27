using Database.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Core.Exceptions;
using Project.Service.Tests.Factories;
using Project.Service.Tests.Fixtures;
using Project.Services.PostService;
using Xunit;

namespace Project.Service.Tests;

[Collection("Database")]
public class PostServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly PostService _postService;
    private Guid _companyId;

    public PostServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;

        var postRepository = new PostRepository(
            _fixture.Context,
            NullLogger<PostRepository>.Instance);

        _postService = new PostService(
            postRepository,
            NullLogger<PostService>.Instance);
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

        var companyRepo = new CompanyRepository(
            _fixture.Context,
            NullLogger<CompanyRepository>.Instance);
        var company = CompanyObjectFabric.CreateValidCreationCompany();
        var created = await companyRepo.AddCompanyAsync(company);
        _companyId = created.CompanyId;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ===== AddPostAsync =====

    [Fact]
    public async Task AddPostAsync_ValidData_ReturnsCreatedPost()
    {
        // Arrange
        var creation = PostObjectFabric.CreateValidCreatePost(_companyId);

        // Act
        var result = await _postService.AddPostAsync(creation.Title, creation.Salary, _companyId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(creation.Title, result.Title);
        Assert.Equal(creation.Salary, result.Salary);
        Assert.Equal(_companyId, result.CompanyId);
    }

    [Fact]
    public async Task AddPostAsync_EmptyTitle_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _postService.AddPostAsync("", 100000, _companyId));
    }

    [Fact]
    public async Task AddPostAsync_ZeroSalary_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _postService.AddPostAsync("Разработчик", 0, _companyId));
    }

    [Fact]
    public async Task AddPostAsync_NegativeSalary_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _postService.AddPostAsync("Разработчик", -50000, _companyId));
    }

    // ===== GetPostByIdAsync =====

    [Fact]
    public async Task GetPostByIdAsync_Existing_ReturnsPost()
    {
        // Arrange
        var creation = PostObjectFabric.CreateValidCreatePost(_companyId);
        var created = await _postService.AddPostAsync(creation.Title, creation.Salary, _companyId);

        // Act
        var result = await _postService.GetPostByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal(creation.Title, result.Title);
    }

    [Fact]
    public async Task GetPostByIdAsync_NonExistent_ThrowsPostNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<PostNotFoundException>(() =>
            _postService.GetPostByIdAsync(nonExistentId));
    }

    // ===== UpdatePostAsync =====

    [Fact]
    public async Task UpdatePostAsync_ValidData_ReturnsUpdatedPost()
    {
        // Arrange
        var creation = PostObjectFabric.CreateValidCreatePost(_companyId);
        var created = await _postService.AddPostAsync(creation.Title, creation.Salary, _companyId);

        // Act
        var result = await _postService.UpdatePostAsync(created.Id, "Старший разработчик", 200000);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Старший разработчик", result.Title);
        Assert.Equal(200000, result.Salary);
    }

    [Fact]
    public async Task UpdatePostAsync_NonExistent_ThrowsPostNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<PostNotFoundException>(() =>
            _postService.UpdatePostAsync(nonExistentId, "Новое название"));
    }

    // ===== GetPostsByCompanyIdAsync =====

    [Fact]
    public async Task GetPostsByCompanyIdAsync_WithData_ReturnsPosts()
    {
        // Arrange
        var p1 = PostObjectFabric.CreateValidCreatePost(_companyId);
        var p2 = PostObjectFabric.CreateValidCreatePost(_companyId);
        await _postService.AddPostAsync(p1.Title, p1.Salary, _companyId);
        await _postService.AddPostAsync(p2.Title, p2.Salary, _companyId);

        // Act
        var result = await _postService.GetPostsByCompanyIdAsync(_companyId, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count() >= 2);
    }

    [Fact]
    public async Task GetPostsByCompanyIdAsync_NoData_ReturnsEmptyList()
    {
        // Arrange
        var unknownCompanyId = Guid.NewGuid();

        // Act
        var result = await _postService.GetPostsByCompanyIdAsync(unknownCompanyId, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // ===== DeletePostAsync =====

    [Fact]
    public async Task DeletePostAsync_Existing_DeletesSuccessfully()
    {
        // Arrange
        var creation = PostObjectFabric.CreateValidCreatePost(_companyId);
        var created = await _postService.AddPostAsync(creation.Title, creation.Salary, _companyId);

        // Act
        await _postService.DeletePostAsync(created.Id);

        // Assert
        await Assert.ThrowsAsync<PostNotFoundException>(() =>
            _postService.GetPostByIdAsync(created.Id));
    }

    [Fact]
    public async Task DeletePostAsync_NonExistent_ThrowsPostNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<PostNotFoundException>(() =>
            _postService.DeletePostAsync(nonExistentId));
    }
}
