using System.Net;
using System.Net.Http.Json;
using Project.Dto.Http.External;
using Project.E2E.Tests.Infrastructure;
using Xunit;

namespace Project.E2E.Tests;

[Collection(E2ECollection.Name)]
public sealed class ExternalAgeServiceE2E
{
    private readonly HttpClient _client;

    public ExternalAgeServiceE2E(E2EEnvironmentFixture fixture)
    {
        _client = new HttpClient { BaseAddress = new Uri(fixture.BaseApiUrl) };
    }

    [Fact]
    public async Task ExternalAgePrediction_ReturnsData_ForConfiguredProvider()
    {
        var name = Environment.GetEnvironmentVariable("E2E_EXTERNAL_TEST_NAME") ?? "ivan";
        var expectMock = bool.TryParse(Environment.GetEnvironmentVariable("E2E_EXTERNAL_EXPECT_MOCK"), out var parsed) && parsed;

        var response = await _client.GetAsync($"api/v1/external/age-prediction?name={Uri.EscapeDataString(name)}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<AgePredictionDto>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Source));

        if (expectMock)
        {
            Assert.Equal("mock", payload.Source);
            Assert.Equal(31, payload.Age);
        }
        else
        {
            Assert.Equal("real", payload.Source);
            Assert.True(payload.Age is null or >= 0);
        }
    }
}
