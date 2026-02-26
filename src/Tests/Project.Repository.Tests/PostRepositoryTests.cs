using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Core.Exceptions;
using Project.Core.Models.Post;
using Project.Repository.Tests.Factories;
using Project.Repository.Tests.Fixtures;
using Xunit;

namespace Project.Repository.Tests;

[Collection("Database")]
public class PostRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly PostRepository _repository;
    private Guid _companyId;

    public PostRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new PostRepository(
            _fixture.Context,
            NullLogger<PostRepository>.Instance);
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        // Создаём компанию, необходимую для всех тестов
        var company = CompanyDbObjectFabric.CreateValidCompanyDb("Post Test Company");
        await _fixture.Context.CompanyDb.AddAsync(company);
        await _fixture.Context.SaveChangesAsync();
        _companyId = company.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ===== AddPostAsync =====

    [Fact]
    public async Task AddPostAsync_ShouldAddPost_WhenNoDuplicateExists()
    {
        // Arrange
        var post = new CreatePost("Senior Developer", 100000, _companyId);

        // Act
        var result = await _repository.AddPostAsync(post);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Senior Developer", result.Title);
        Assert.Equal(100000, result.Salary);
        Assert.Equal(_companyId, result.CompanyId);

        var dbRecord = await _fixture.Context.PostDb.FindAsync(result.Id);
        Assert.NotNull(dbRecord);
    }

    [Fact]
    public async Task AddPostAsync_ShouldThrow_WhenDuplicateTitleInSameCompany()
    {
        // Arrange
        await _repository.AddPostAsync(new CreatePost("Developer", 80000, _companyId));
        var duplicate = new CreatePost("Developer", 85000, _companyId);

        // Act & Assert
        await Assert.ThrowsAsync<PostAlreadyExistsException>(() =>
            _repository.AddPostAsync(duplicate));
    }

    [Fact]
    public async Task AddPostAsync_ShouldAllowSameTitle_InDifferentCompanies()
    {
        // Arrange
        var company2 = CompanyDbObjectFabric.CreateValidCompanyDb("Рога и копыта");
        await _fixture.Context.CompanyDb.AddAsync(company2);
        await _fixture.Context.SaveChangesAsync();

        await _repository.AddPostAsync(new CreatePost("Manager", 90000, _companyId));

        // Act
        var result = await _repository.AddPostAsync(new CreatePost("Manager", 95000, company2.Id));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(company2.Id, result.CompanyId);
    }

    // ===== GetPostByIdAsync =====

    [Fact]
    public async Task GetPostByIdAsync_ShouldReturnPost_WhenExists()
    {
        // Arrange
        var addedPost = await _repository.AddPostAsync(new CreatePost("CTO", 150000, _companyId));

        // Act
        var result = await _repository.GetPostByIdAsync(addedPost.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(addedPost.Id, result.Id);
        Assert.Equal("CTO", result.Title);
    }

    [Fact]
    public async Task GetPostByIdAsync_ShouldThrow_WhenNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<PostNotFoundException>(() =>
            _repository.GetPostByIdAsync(Guid.NewGuid()));
    }

    // ===== UpdatePostAsync =====

    [Fact]
    public async Task UpdatePostAsync_ShouldUpdateFields_WhenValid()
    {
        // Arrange
        var addedPost = await _repository.AddPostAsync(
            new CreatePost("Junior Developer", 60000, _companyId));

        var update = new UpdatePost(addedPost.Id, "Senior Developer", 90000);

        // Act
        var result = await _repository.UpdatePostAsync(update);

        // Assert
        Assert.Equal("Senior Developer", result.Title);
        Assert.Equal(90000, result.Salary);

        var dbRecord = await _fixture.Context.PostDb.FindAsync(addedPost.Id);
        Assert.Equal("Senior Developer", dbRecord!.Title);
        Assert.Equal(90000, dbRecord.Salary);
    }

    [Fact]
    public async Task UpdatePostAsync_ShouldThrow_WhenDuplicateTitleInSameCompany()
    {
        // Arrange
        await _repository.AddPostAsync(new CreatePost("Designer", 70000, _companyId));
        var post2 = await _repository.AddPostAsync(new CreatePost("Developer", 80000, _companyId));

        var update = new UpdatePost(post2.Id, "Designer", 85000);

        // Act & Assert
        await Assert.ThrowsAsync<PostAlreadyExistsException>(() =>
            _repository.UpdatePostAsync(update));
    }

    // ===== GetPostsAsync =====

    [Fact]
    public async Task GetPostsAsync_ShouldReturnPagedResults()
    {
        // Arrange
        for (var i = 0; i < 15; i++)
            await _repository.AddPostAsync(
                new CreatePost($"Position {i}", 50000 + i * 1000, _companyId));

        // Act
        var page1 = await _repository.GetPostsAsync(_companyId, 1, 5);
        var page2 = await _repository.GetPostsAsync(_companyId, 2, 5);

        // Assert
        var page1List = page1.ToList();
        var page2List = page2.ToList();
        Assert.Equal(5, page1List.Count);
        Assert.Equal(5, page2List.Count);
        Assert.NotEqual(page1List.First().Id, page2List.First().Id);
    }

    // ===== DeletePostAsync =====

    [Fact]
    public async Task DeletePostAsync_ShouldRemovePost_WhenExists()
    {
        // Arrange
        var addedPost = await _repository.AddPostAsync(
            new CreatePost("CEO", 200000, _companyId));

        // Act
        await _repository.DeletePostAsync(addedPost.Id);

        // Assert
        await Assert.ThrowsAsync<PostNotFoundException>(() =>
            _repository.GetPostByIdAsync(addedPost.Id));
    }

    [Fact]
    public async Task DeletePostAsync_ShouldThrow_WhenNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<PostNotFoundException>(() =>
            _repository.DeletePostAsync(Guid.NewGuid()));
    }
}
