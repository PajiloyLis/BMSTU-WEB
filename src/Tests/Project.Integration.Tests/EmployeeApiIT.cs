using System.Net;
using System.Net.Http.Json;
using Project.Dto.Http;
using Project.Dto.Http.Employee;
using Project.Integration.Tests.Factories;
using Project.Integration.Tests.Infrastructure;
using Xunit;

namespace Project.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public sealed class EmployeeApiIT : IAsyncLifetime
{
    private readonly PostgresContainerFixture _dbFixture;
    private readonly HttpClient _client;
    private readonly IntegrationTestWebAppFactory _factory;

    public EmployeeApiIT(PostgresContainerFixture dbFixture)
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
    public async Task CreateAndGetEmployee_ShouldReturnCreatedEntity()
    {
        var request = EmployeeObjectFabric.CreateEmployeeDto();

        var createResponse = await _client.PostAsJsonAsync("/api/v1/employees", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<EmployeeDto>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.EmployeeId);
        Assert.Equal(request.FullName, created.FullName);


        var getResponse = await _client.GetAsync($"/api/v1/employees/{created.EmployeeId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<EmployeeDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.EmployeeId, fetched.EmployeeId);
        Assert.Equal(created.Email, fetched.Email);
    }

    [Fact]
    public async Task UpdateEmployee_ShouldReturnUpdatedEntity()
    {
        var update = EmployeeObjectFabric.UpdateEmployeeDto();

        var updateResponse = await _client.PatchAsJsonAsync($"/api/v1/employees/{PostgresContainerFixture.SeedEmployeeId}", update);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<EmployeeDto>();
        Assert.NotNull(updated);
        Assert.Equal(update.FullName, updated.FullName);
        Assert.Equal(update.Email, updated.Email);
    }

    [Fact]
    public async Task UpdateEmployee_WithInvalidEmail_ShouldReturnBadRequest()
    {
        var invalid = EmployeeObjectFabric.UpdateEmployeeDto(email: "invalid-email");

        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/employees/{PostgresContainerFixture.SeedEmployeeId}",
            invalid);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal(nameof(ArgumentException), error.ErrorType);
    }

    [Fact]
    public async Task DeleteEmployee_ShouldReturnNoContent_AndThenNotFound()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/employees", EmployeeObjectFabric.CreateEmployeeDto());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<EmployeeDto>();
        Assert.NotNull(created);

        var deleteResponse = await _client.DeleteAsync($"/api/v1/employees/{created.EmployeeId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1/employees/{created.EmployeeId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteEmployee_WhenNotFound_ShouldReturnNotFound()
    {
        var missingId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"/api/v1/employees/{missingId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("EmployeeNotFoundException", error.ErrorType);
    }

    [Fact]
    public async Task CreateEmployee_WithInvalidEmail_ShouldReturnBadRequest()
    {
        var invalid = EmployeeObjectFabric.CreateEmployeeDto(email: "invalid-email");

        var response = await _client.PostAsJsonAsync("/api/v1/employees", invalid);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal(nameof(ArgumentException), error.ErrorType);
    }
}

