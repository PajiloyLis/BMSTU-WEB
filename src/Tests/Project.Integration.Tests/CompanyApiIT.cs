using System.Net;
using System.Net.Http.Json;
using Project.Dto.Http;
using Project.Dto.Http.Company;
using Project.Integration.Tests.Factories;
using Project.Integration.Tests.Infrastructure;
using Xunit;

namespace Project.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public sealed class CompanyApiIT : IAsyncLifetime
{
    private static readonly Guid SeedCompanyId = Guid.Parse("fa001e78-8ff1-4bb3-b417-d518483ca7b3");

    private readonly PostgresContainerFixture _dbFixture;
    private readonly HttpClient _client;
    private readonly IntegrationTestWebAppFactory _factory;

    public CompanyApiIT(PostgresContainerFixture dbFixture)
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
    public async Task CreateAndGetCompany_ShouldReturnCreatedEntity()
    {
        var request = CompanyObjectFabric.CreateCompanyDto();

        var createResponse = await _client.PostAsJsonAsync("/api/v1/companies", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CompanyDto>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.CompanyId);
        Assert.Equal(request.Title, created.Title);

        var getResponse = await _client.GetAsync($"/api/v1/companies/{created.CompanyId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<CompanyDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.CompanyId, fetched.CompanyId);
        Assert.Equal(created.Email, fetched.Email);
    }

    [Fact]
    public async Task UpdateCompany_ShouldReturnUpdatedEntity()
    {
        var update = CompanyObjectFabric.UpdateCompanyDto();

        var updateResponse = await _client.PatchAsJsonAsync($"/api/v1/companies/{SeedCompanyId}", update);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CompanyDto>();
        Assert.NotNull(updated);
        Assert.Equal(update.Title, updated.Title);
        Assert.Equal(update.Email, updated.Email);
    }

    [Fact]
    public async Task UpdateCompany_WithInvalidEmail_ShouldReturnBadRequest()
    {
        var invalid = CompanyObjectFabric.UpdateCompanyDto(email: "invalid-email");

        var response = await _client.PatchAsJsonAsync($"/api/v1/companies/{SeedCompanyId}", invalid);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal(nameof(ArgumentException), error.ErrorType);
    }

    [Fact]
    public async Task DeleteCompany_ShouldReturnNoContent_AndMarkAsDeleted()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/companies", CompanyObjectFabric.CreateCompanyDto());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CompanyDto>();
        Assert.NotNull(created);

        var deleteResponse = await _client.DeleteAsync($"/api/v1/companies/{created.CompanyId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1/companies/{created.CompanyId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var company = await getResponse.Content.ReadFromJsonAsync<CompanyDto>();
        Assert.NotNull(company);
        Assert.True(company.IsDeleted);
    }

    [Fact]
    public async Task DeleteCompany_WhenNotFound_ShouldReturnNotFound()
    {
        var missingId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"/api/v1/companies/{missingId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("CompanyNotFoundException", error.ErrorType);
    }

    [Fact]
    public async Task CreateCompany_WithInvalidEmail_ShouldReturnBadRequest()
    {
        var invalid = CompanyObjectFabric.CreateCompanyDto(email: "invalid-email");

        var response = await _client.PostAsJsonAsync("/api/v1/companies", invalid);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal(nameof(ArgumentException), error.ErrorType);
    }
}

