using System.Net;
using System.Net.Http.Json;
using Project.Dto.Http;
using Project.Dto.Http.PostHistory;
using Project.Integration.Tests.Factories;
using Project.Integration.Tests.Infrastructure;
using Xunit;

namespace Project.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public sealed class PostHistoryApiIT : IAsyncLifetime
{
    private static readonly Guid SeedEmployeeId = Guid.Parse("bad8a5a0-ec08-412e-8f19-0d9e993d5651");
    private static readonly Guid SeedPostId = Guid.Parse("d7aac778-85f0-4953-897e-a5689da272e4");

    private readonly PostgresContainerFixture _dbFixture;
    private readonly HttpClient _client;
    private readonly IntegrationTestWebAppFactory _factory;

    public PostHistoryApiIT(PostgresContainerFixture dbFixture)
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
    public async Task CreateAndGetPostHistory_ShouldReturnCreatedEntity()
    {
        var createRequest = PostHistoryObjectFabric.CreatePostHistoryDto(
            postId: Guid.Parse("139d4502-cd99-4c29-846d-cb5dccabee1a"),
            employeeId: SeedEmployeeId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/postHistories", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<PostHistoryDto>();
        Assert.NotNull(created);
        Assert.Equal(createRequest.PostId, created.PostId);
        Assert.Equal(createRequest.EmployeeId, created.EmployeeId);

        var getResponse = await _client.GetAsync($"/api/v1/employees/{created.EmployeeId}/postHistories/{created.PostId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<PostHistoryDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.StartDate, fetched.StartDate);
    }

    [Fact]
    public async Task UpdatePostHistory_ShouldReturnUpdatedEntity()
    {
        var update = PostHistoryObjectFabric.UpdatePostHistoryDto(
            startDate: new DateOnly(1999, 1, 1),
            endDate: new DateOnly(2004, 1, 1));

        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/employees/{SeedEmployeeId}/postHistories/{SeedPostId}",
            update);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<PostHistoryDto>();
        Assert.NotNull(updated);
        Assert.Equal(update.StartDate, updated.StartDate);
        Assert.Equal(update.EndDate, updated.EndDate);
    }

    [Fact]
    public async Task UpdatePostHistory_WhenNotFound_ShouldReturnNotFound()
    {
        var missingPostId = Guid.NewGuid();
        var update = PostHistoryObjectFabric.UpdatePostHistoryDto();

        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/employees/{SeedEmployeeId}/postHistories/{missingPostId}",
            update);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("PostHistoryNotFoundException", error.ErrorType);
    }

    [Fact]
    public async Task DeletePostHistory_ShouldReturnNoContent_AndThenNotFound()
    {
        var createRequest = PostHistoryObjectFabric.CreatePostHistoryDto(
            postId: Guid.Parse("139d4502-cd99-4c29-846d-cb5dccabee1a"),
            employeeId: SeedEmployeeId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/postHistories", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync(
            $"/api/v1/employees/{createRequest.EmployeeId}/postHistories/{createRequest.PostId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync(
            $"/api/v1/employees/{createRequest.EmployeeId}/postHistories/{createRequest.PostId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeletePostHistory_WhenNotFound_ShouldReturnNotFound()
    {
        var response = await _client.DeleteAsync(
            $"/api/v1/employees/{SeedEmployeeId}/postHistories/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("PostHistoryNotFoundException", error.ErrorType);
    }

    [Fact]
    public async Task CreatePostHistory_WithInvalidDates_ShouldReturnBadRequest()
    {
        var invalid = PostHistoryObjectFabric.CreatePostHistoryDto(
            SeedPostId,
            SeedEmployeeId,
            startDate: DateOnly.FromDateTime(DateTime.UtcNow),
            endDate: null);

        var response = await _client.PostAsJsonAsync("/api/v1/postHistories", invalid);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal(nameof(ArgumentException), error.ErrorType);
    }

    [Fact]
    public async Task GetPostHistoriesByEmployeeId_ShouldReturnNonEmptyCollection()
    {
        var response = await _client.GetAsync(
            $"/api/v1/employees/{SeedEmployeeId}/postHistories?pageNumber=1&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<PostHistoryDto>>();
        Assert.NotNull(items);
        Assert.NotEmpty(items);
    }
}

