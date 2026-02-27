using System.Net;
using System.Net.Http.Json;
using Project.Dto.Http;
using Project.Dto.Http.Education;
using Project.Integration.Tests.Factories;
using Project.Integration.Tests.Infrastructure;
using Xunit;

namespace Project.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public sealed class EducationApiIT : IAsyncLifetime
{
    private static readonly Guid SeedEmployeeId = Guid.Parse("bad8a5a0-ec08-412e-8f19-0d9e993d5651");
    private static readonly Guid SeedEducationId = Guid.Parse("526b802f-6654-4569-937b-a594a83f5217");

    private readonly PostgresContainerFixture _dbFixture;
    private readonly HttpClient _client;
    private readonly IntegrationTestWebAppFactory _factory;

    public EducationApiIT(PostgresContainerFixture dbFixture)
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
    public async Task CreateAndGetEducation_ShouldReturnCreatedEntity()
    {
        var request = EducationObjectFabric.CreateEducationDto(SeedEmployeeId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/educations", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<EducationDto>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(request.Institution, created.Institution);

        var getResponse = await _client.GetAsync($"/api/v1/educations/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<EducationDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.Level, fetched.Level);
    }

    [Fact]
    public async Task UpdateEducation_ShouldReturnUpdatedEntity()
    {
        var update = EducationObjectFabric.UpdateEducationDto(SeedEmployeeId);

        var response = await _client.PatchAsJsonAsync($"/api/v1/educations/{SeedEducationId}", update);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EducationDto>();
        Assert.NotNull(updated);
        Assert.Equal(update.Institution, updated.Institution);
        Assert.Equal(update.Level, updated.Level);
    }

    [Fact]
    public async Task UpdateEducation_WhenNotFound_ShouldReturnNotFound()
    {
        var missingId = Guid.NewGuid();
        var update = EducationObjectFabric.UpdateEducationDto(SeedEmployeeId);

        var response = await _client.PatchAsJsonAsync($"/api/v1/educations/{missingId}", update);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("EducationNotFoundException", error.ErrorType);
    }

    [Fact]
    public async Task DeleteEducation_ShouldReturnNoContent_AndThenNotFound()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/educations",
            EducationObjectFabric.CreateEducationDto(SeedEmployeeId));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<EducationDto>();
        Assert.NotNull(created);

        var deleteResponse = await _client.DeleteAsync($"/api/v1/educations/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1/educations/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteEducation_WhenNotFound_ShouldReturnNotFound()
    {
        var missingId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"/api/v1/educations/{missingId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("EducationNotFoundException", error.ErrorType);
    }

    [Fact]
    public async Task CreateEducation_WithInvalidLevel_ShouldReturnBadRequest()
    {
        var invalid = EducationObjectFabric.CreateEducationDto(SeedEmployeeId, level: "INVALID_LEVEL");

        var response = await _client.PostAsJsonAsync("/api/v1/educations", invalid);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("EducationLevelNotFoundException", error.ErrorType);
    }

    [Fact]
    public async Task GetEducationsByEmployeeId_ShouldReturnNonEmptyCollection()
    {
        var response = await _client.GetAsync($"/api/v1/employees/{SeedEmployeeId}/educations?pageNumber=1&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var educations = await response.Content.ReadFromJsonAsync<List<EducationDto>>();
        Assert.NotNull(educations);
        Assert.NotEmpty(educations);
    }
}

