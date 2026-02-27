using System.Net;
using System.Net.Http.Json;
using Project.Dto.Http;
using Project.Dto.Http.Post;
using Project.Integration.Tests.Factories;
using Project.Integration.Tests.Infrastructure;
using Xunit;

namespace Project.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public sealed class PostApiIT : IAsyncLifetime
{
    private static readonly Guid SeedCompanyId = Guid.Parse("fa001e78-8ff1-4bb3-b417-d518483ca7b3");
    private static readonly Guid SeedPostId = Guid.Parse("d7aac778-85f0-4953-897e-a5689da272e4");

    private readonly PostgresContainerFixture _dbFixture;
    private readonly HttpClient _client;
    private readonly IntegrationTestWebAppFactory _factory;

    public PostApiIT(PostgresContainerFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new IntegrationTestWebAppFactory(_dbFixture.ConnectionString);
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _dbFixture.ResetToBaselineAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbFixture.ResetToBaselineAsync();
    }

    [Fact]
    public async Task CreateAndGetPost_ShouldReturnCreatedEntity()
    {
        var request = PostObjectFabric.CreatePostDto(SeedCompanyId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/posts", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<PostDto>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(request.Title, created.Title);
        Assert.Equal(request.CompanyId, created.CompanyId);

        var getResponse = await _client.GetAsync($"/api/v1/posts/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<PostDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.Title, fetched.Title);
    }

    [Fact]
    public async Task UpdatePost_ShouldReturnUpdatedEntity()
    {
        var update = PostObjectFabric.UpdatePostDto();

        var response = await _client.PatchAsJsonAsync($"/api/v1/posts/{SeedPostId}", update);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<PostDto>();
        Assert.NotNull(updated);
        Assert.Equal(update.Title, updated.Title);
        Assert.Equal(update.Salary, updated.Salary);
    }

    [Fact]
    public async Task UpdatePost_WhenNotFound_ShouldReturnNotFound()
    {
        var missingId = Guid.NewGuid();
        var update = PostObjectFabric.UpdatePostDto();

        var response = await _client.PatchAsJsonAsync($"/api/v1/posts/{missingId}", update);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("PostNotFoundException", error.ErrorType);
    }

    [Fact]
    public async Task DeletePost_ShouldReturnNoContent_AndThenNotFound()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/posts", PostObjectFabric.CreatePostDto(SeedCompanyId));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<PostDto>();
        Assert.NotNull(created);

        var deleteResponse = await _client.DeleteAsync($"/api/v1/posts/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1/posts/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeletePost_WhenNotFound_ShouldReturnNotFound()
    {
        var missingId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"/api/v1/posts/{missingId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("PostNotFoundException", error.ErrorType);
    }

    [Fact]
    public async Task CreatePost_WithInvalidSalary_ShouldReturnBadRequest()
    {
        var invalid = PostObjectFabric.CreatePostDto(SeedCompanyId, salary: 0m);

        var response = await _client.PostAsJsonAsync("/api/v1/posts", invalid);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal(nameof(ArgumentException), error.ErrorType);
    }

    [Fact]
    public async Task GetPostsByCompanyId_ShouldReturnNonEmptyCollection()
    {
        var response = await _client.GetAsync($"/api/v1/companies/{SeedCompanyId}/posts?pageNumber=1&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var posts = await response.Content.ReadFromJsonAsync<List<PostDto>>();
        Assert.NotNull(posts);
        Assert.NotEmpty(posts);
    }
}

