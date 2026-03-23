using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Project.Dto.Http.External;
using Project.Dto.Http.User;
using Project.E2E.Tests.Infrastructure;
using Xunit;

namespace Project.E2E.Tests;

[Collection(E2ECollection.Name)]
public sealed class ExternalAgeServiceE2E : IAsyncLifetime
{
    private const string ApiPrefix = "api/v1/";
    private const string SeedEmployeeEmail = "fedorova@example.com";
    private const string SeedEmployeePassword = "fedorova";

    private readonly E2EEnvironmentFixture _fixture;
    private readonly HttpClient _client;

    public ExternalAgeServiceE2E(E2EEnvironmentFixture fixture)
    {
        _fixture = fixture;
        _client = new HttpClient { BaseAddress = new Uri(_fixture.BaseApiUrl) };
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _client.Dispose();
    }

    [Fact]
    public async Task ExternalAge_ReturnsAge_ForName()
    {
        var authData = await RegisterAndAuthorizeAsync();

        var name = Environment.GetEnvironmentVariable("E2E_EXTERNAL_TEST_NAME") ?? "ivan";

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authData.Token);

        var response = await _client.GetAsync($"{ApiPrefix}external/age?name={Uri.EscapeDataString(name)}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<AgePredictionDto>();
        Assert.NotNull(dto);
        Assert.Equal(name, dto!.Name);

        Assert.True(dto.Age.HasValue, "Age must be present");
        Assert.InRange(dto.Age!.Value, 1, 120);

        if (dto.Count.HasValue)
            Assert.True(dto.Count.Value > 0, "Count must be positive when present");
    }

    private async Task<AuthorizationDataDto> RegisterAndAuthorizeAsync()
    {
        var registerResponse = await _client.PostAsJsonAsync(
            $"{ApiPrefix}auth/register",
            new LoginDto(SeedEmployeePassword, SeedEmployeeEmail));

        if (registerResponse.StatusCode != HttpStatusCode.OK)
        {
            var body = await registerResponse.Content.ReadAsStringAsync();
            // В интеграционных тестах возможна гонка/повторный прогон, поэтому допускаем "already exists"
            Assert.True(registerResponse.StatusCode is HttpStatusCode.Conflict,
                $"Register failed. Status={(int)registerResponse.StatusCode} {registerResponse.StatusCode}. Body={body}");
        }

        var loginResponse = await _client.PostAsJsonAsync(
            $"{ApiPrefix}auth/login",
            new LoginDto(SeedEmployeePassword, SeedEmployeeEmail));

        if (loginResponse.StatusCode != HttpStatusCode.OK)
        {
            var loginBody = await loginResponse.Content.ReadAsStringAsync();
            Assert.Fail($"Login failed. Status={(int)loginResponse.StatusCode} {loginResponse.StatusCode}. Body={loginBody}");
        }

        var authData = await loginResponse.Content.ReadFromJsonAsync<AuthorizationDataDto>();
        Assert.NotNull(authData);
        Assert.False(string.IsNullOrWhiteSpace(authData!.Token));
        return authData;
    }
}

