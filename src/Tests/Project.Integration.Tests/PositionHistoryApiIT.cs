using System.Net;
using System.Net.Http.Json;
using Project.Dto.Http;
using Project.Dto.Http.PositionHistory;
using Project.Integration.Tests.Factories;
using Project.Integration.Tests.Infrastructure;
using Xunit;

namespace Project.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public sealed class PositionHistoryApiIT : IAsyncLifetime
{
    private static readonly Guid SeedEmployeeId = Guid.Parse("bad8a5a0-ec08-412e-8f19-0d9e993d5651");
    private static readonly Guid SeedPositionId = Guid.Parse("fa001e78-0001-4000-8000-000000000001");

    private readonly PostgresContainerFixture _dbFixture;
    private readonly HttpClient _client;
    private readonly IntegrationTestWebAppFactory _factory;

    public PositionHistoryApiIT(PostgresContainerFixture dbFixture)
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
    public async Task CreateAndGetPositionHistory_ShouldReturnCreatedEntity()
    {
        var createRequest = PositionHistoryObjectFabric.CreatePositionHistoryDto(
            positionId: Guid.Parse("fa001e78-0002-4000-8000-000000000002"),
            employeeId: SeedEmployeeId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/positionHistories", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<PositionHistoryDto>();
        Assert.NotNull(created);
        Assert.Equal(createRequest.PositionId, created.PositionId);
        Assert.Equal(createRequest.EmployeeId, created.EmployeeId);

        var getResponse = await _client.GetAsync(
            $"/api/v1/employees/{created.EmployeeId}/positionHistories/{created.PositionId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<PositionHistoryDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.StartDate, fetched.StartDate);
    }

    [Fact]
    public async Task UpdatePositionHistory_ShouldReturnUpdatedEntity()
    {
        var update = PositionHistoryObjectFabric.UpdatePositionHistoryDto(
            startDate: new DateOnly(1999, 1, 1),
            endDate: new DateOnly(2004, 1, 1));

        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/employees/{SeedEmployeeId}/positionHistories/{SeedPositionId}",
            update);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<PositionHistoryDto>();
        Assert.NotNull(updated);
        Assert.Equal(update.StartDate, updated.StartDate);
        Assert.Equal(update.EndDate, updated.EndDate);
    }

    [Fact]
    public async Task UpdatePositionHistory_WhenNotFound_ShouldReturnNotFound()
    {
        var missingPositionId = Guid.NewGuid();
        var update = PositionHistoryObjectFabric.UpdatePositionHistoryDto();

        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/employees/{SeedEmployeeId}/positionHistories/{missingPositionId}",
            update);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("PositionHistoryNotFoundException", error.ErrorType);
    }

    [Fact]
    public async Task DeletePositionHistory_ShouldReturnNoContent_AndThenNotFound()
    {
        var createRequest = PositionHistoryObjectFabric.CreatePositionHistoryDto(
            positionId: Guid.Parse("fa001e78-0002-4000-8000-000000000002"),
            employeeId: SeedEmployeeId);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/positionHistories", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync(
            $"/api/v1/employees/{createRequest.EmployeeId}/positionHistories/{createRequest.PositionId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync(
            $"/api/v1/employees/{createRequest.EmployeeId}/positionHistories/{createRequest.PositionId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeletePositionHistory_WhenNotFound_ShouldReturnNotFound()
    {
        var response = await _client.DeleteAsync(
            $"/api/v1/employees/{SeedEmployeeId}/positionHistories/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("PositionHistoryNotFoundException", error.ErrorType);
    }

    [Fact]
    public async Task CreatePositionHistory_WithInvalidDates_ShouldReturnBadRequest()
    {
        var invalid = PositionHistoryObjectFabric.CreatePositionHistoryDto(
            SeedPositionId,
            SeedEmployeeId,
            startDate: DateOnly.FromDateTime(DateTime.UtcNow),
            endDate: null);

        var response = await _client.PostAsJsonAsync("/api/v1/positionHistories", invalid);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal(nameof(ArgumentException), error.ErrorType);
    }

    [Fact]
    public async Task GetPositionHistoriesByEmployeeId_ShouldReturnNonEmptyCollection()
    {
        var response = await _client.GetAsync(
            $"/api/v1/employees/{SeedEmployeeId}/positionHistories?pageNumber=1&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<PositionHistoryDto>>();
        Assert.NotNull(items);
        Assert.NotEmpty(items);
    }
}

