using System.Net;
using System.Net.Http.Json;
using Project.Dto.Http;
using Project.Dto.Http.Position;
using Project.Integration.Tests.Factories;
using Project.Integration.Tests.Infrastructure;
using Xunit;

namespace Project.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public sealed class PositionApiIT : IAsyncLifetime
{
    private static readonly Guid SeedCompanyId = Guid.Parse("fa001e78-8ff1-4bb3-b417-d518483ca7b3");
    private static readonly Guid SeedHeadPositionId = Guid.Parse("fa001e78-0001-4000-8000-000000000001");

    private readonly PostgresContainerFixture _dbFixture;
    private readonly HttpClient _client;
    private readonly IntegrationTestWebAppFactory _factory;

    public PositionApiIT(PostgresContainerFixture dbFixture)
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
    public async Task CreateAndGetPosition_ShouldReturnCreatedEntity()
    {
        var request = PositionObjectFabric.CreatePositionDto(SeedCompanyId, parentId: SeedHeadPositionId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/positions", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<PositionDto>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(request.Title, created.Title);
        Assert.Equal(request.CompanyId, created.CompanyId);

        var getResponse = await _client.GetAsync($"/api/v1/positions/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<PositionDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.Title, fetched.Title);
    }

    [Fact]
    public async Task CreatePosition_WithInvalidTitle_ShouldReturnBadRequest()
    {
        var invalid = PositionObjectFabric.CreatePositionDto(SeedCompanyId, title: "   ");

        var response = await _client.PostAsJsonAsync("/api/v1/positions", invalid);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal(nameof(ArgumentException), error.ErrorType);
    }

    [Fact]
    public async Task DeletePosition_ShouldReturnNoContent_AndThenNotFound()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/positions",
            PositionObjectFabric.CreatePositionDto(SeedCompanyId, parentId: SeedHeadPositionId));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<PositionDto>();
        Assert.NotNull(created);

        var deleteResponse = await _client.DeleteAsync($"/api/v1/positions/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1/positions/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeletePosition_WhenNotFound_ShouldReturnNotFound()
    {
        var missingId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"/api/v1/positions/{missingId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("PositionNotFoundException", error.ErrorType);
    }

    [Fact]
    public async Task UpdatePositionTitle_ForExistingPosition_ShouldReturnUpdatedTitle()
    {
        const string newTitle = "Генеральный директор Updated";

        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/positions/{SeedHeadPositionId}/title",
            newTitle);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<PositionDto>();
        Assert.NotNull(updated);
        Assert.Equal(newTitle, updated.Title);
    }

    [Fact]
    public async Task GetHeadPositionByCompanyId_ShouldReturnHead()
    {
        var response = await _client.GetAsync($"/api/v1/companies/{SeedCompanyId}/headPosition");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var head = await response.Content.ReadFromJsonAsync<PositionDto>();
        Assert.NotNull(head);
        Assert.Equal(SeedHeadPositionId, head.Id);
    }

    [Fact]
    public async Task GetSubordinates_ShouldReturnNonEmptyHierarchy()
    {
        var response = await _client.GetAsync($"/api/v1/positions/{SeedHeadPositionId}/subordinates");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var subordinates = await response.Content.ReadFromJsonAsync<List<PositionHierarchyDto>>();
        Assert.NotNull(subordinates);
        Assert.NotEmpty(subordinates);
    }
}

