using System.Net;
using System.Net.Http.Json;
using Project.Dto.Http;
using Project.Dto.Http.Score;
using Project.Integration.Tests.Factories;
using Project.Integration.Tests.Infrastructure;
using Xunit;

namespace Project.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public sealed class ScoreApiIT : IAsyncLifetime
{
    private static readonly Guid SeedEmployeeId = Guid.Parse("bad8a5a0-ec08-412e-8f19-0d9e993d5651");
    private static readonly Guid SeedAuthorId = Guid.Parse("bf8732c8-208b-4c1e-af7f-a996eb0f1061");
    private static readonly Guid SeedPositionId = Guid.Parse("fa001e78-0001-4000-8000-000000000001");

    private readonly PostgresContainerFixture _dbFixture;
    private readonly HttpClient _client;
    private readonly IntegrationTestWebAppFactory _factory;

    public ScoreApiIT(PostgresContainerFixture dbFixture)
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
    public async Task CreateAndGetScore_ShouldReturnCreatedEntity()
    {
        var request = ScoreObjectFabric.CreateScoreDto(SeedEmployeeId, SeedAuthorId, SeedPositionId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/scores", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ScoreDto>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(request.EmployeeId, created.EmployeeId);
        Assert.Equal(request.PositionId, created.PositionId);

        var getResponse = await _client.GetAsync($"/api/v1/scores/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ScoreDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.EfficiencyScore, fetched.EfficiencyScore);
    }

    [Fact]
    public async Task UpdateScore_ShouldReturnUpdatedEntity()
    {
        var create = await _client.PostAsJsonAsync(
            "/api/v1/scores",
            ScoreObjectFabric.CreateScoreDto(SeedEmployeeId, SeedAuthorId, SeedPositionId));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<ScoreDto>();
        Assert.NotNull(created);

        var update = ScoreObjectFabric.UpdateScoreDto();
        var response = await _client.PatchAsJsonAsync($"/api/v1/scores/{created.Id}", update);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ScoreDto>();
        Assert.NotNull(updated);
        Assert.Equal(update.EfficiencyScore, updated.EfficiencyScore);
        Assert.Equal(update.EngagementScore, updated.EngagementScore);
        Assert.Equal(update.CompetencyScore, updated.CompetencyScore);
    }

    [Fact]
    public async Task UpdateScore_WhenNotFound_ShouldReturnNotFound()
    {
        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/scores/{Guid.NewGuid()}",
            ScoreObjectFabric.UpdateScoreDto());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("ScoreNotFoundException", error.ErrorType);
    }

    [Fact]
    public async Task DeleteScore_ShouldReturnNoContent_AndThenNotFound()
    {
        var create = await _client.PostAsJsonAsync(
            "/api/v1/scores",
            ScoreObjectFabric.CreateScoreDto(SeedEmployeeId, SeedAuthorId, SeedPositionId));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<ScoreDto>();
        Assert.NotNull(created);

        var deleteResponse = await _client.DeleteAsync($"/api/v1/scores/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1/scores/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteScore_WhenNotFound_ShouldReturnNotFound()
    {
        var response = await _client.DeleteAsync($"/api/v1/scores/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("ScoreNotFoundException", error.ErrorType);
    }

    [Fact]
    public async Task CreateScore_WithInvalidEfficiency_ShouldReturnBadRequest()
    {
        var invalid = ScoreObjectFabric.CreateScoreDto(
            SeedEmployeeId,
            SeedAuthorId,
            SeedPositionId,
            efficiencyScore: 6);

        var response = await _client.PostAsJsonAsync("/api/v1/scores", invalid);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal(nameof(ArgumentException), error.ErrorType);
    }

    [Fact]
    public async Task GetScoresByEmployeeId_ShouldReturnNonEmptyCollection()
    {
        var create = await _client.PostAsJsonAsync(
            "/api/v1/scores",
            ScoreObjectFabric.CreateScoreDto(SeedEmployeeId, SeedAuthorId, SeedPositionId));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<ScoreDto>();
        Assert.NotNull(created);

        var response = await _client.GetAsync($"/api/v1/employees/{SeedEmployeeId}/scores?pageNumber=1&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<ScoreDto>>();
        Assert.NotNull(items);
        Assert.Contains(items, s => s.Id == created.Id);
    }

    [Fact]
    public async Task GetScoresByAuthorId_ShouldReturnNonEmptyCollection()
    {
        var create = await _client.PostAsJsonAsync(
            "/api/v1/scores",
            ScoreObjectFabric.CreateScoreDto(SeedEmployeeId, SeedAuthorId, SeedPositionId));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<ScoreDto>();
        Assert.NotNull(created);

        var response = await _client.GetAsync($"/api/v1/employees/scoreAuthor/{SeedAuthorId}/scores?pageNumber=1&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<ScoreDto>>();
        Assert.NotNull(items);
        Assert.Contains(items, s => s.Id == created.Id);
    }

    [Fact]
    public async Task GetScoresByPositionId_ShouldReturnNonEmptyCollection()
    {
        var create = await _client.PostAsJsonAsync(
            "/api/v1/scores",
            ScoreObjectFabric.CreateScoreDto(SeedEmployeeId, SeedAuthorId, SeedPositionId));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<ScoreDto>();
        Assert.NotNull(created);

        var response = await _client.GetAsync($"/api/v1/positions/{SeedPositionId}/scores?pageNumber=1&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<ScoreDto>>();
        Assert.NotNull(items);
        Assert.Contains(items, s => s.Id == created.Id);
    }
}

