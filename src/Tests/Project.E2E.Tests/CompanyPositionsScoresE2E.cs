using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Project.Dto.Http.Company;
using Project.Dto.Http.Position;
using Project.Dto.Http.PositionHistory;
using Project.Dto.Http.Score;
using Project.Dto.Http.User;
using Project.E2E.Tests.Infrastructure;
using Xunit;

namespace Project.E2E.Tests;

[Collection(E2ECollection.Name)]
public sealed class CompanyPositionsScoresE2E : IAsyncLifetime
{
    private const string SeedEmployeeEmail = "fedorova@example.com";
    private const string SeedEmployeePassword = "fedorova";
    private static readonly DateTimeOffset LastMonthBoundary = DateTimeOffset.UtcNow.AddDays(-31);

    private readonly E2EEnvironmentFixture _fixture;
    private readonly HttpClient _client;

    public CompanyPositionsScoresE2E(E2EEnvironmentFixture fixture)
    {
        _fixture = fixture;
        _client = new HttpClient { BaseAddress = new Uri(_fixture.BaseApiUrl + "/") };
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
    public async Task UserFlow_Login_Companies_Positions_CurrentEmployees_LastMonthScores()
    {
        var authData = await RegisterAndAuthorizeAsync();

        var companiesResponse = await _client.GetAsync("companies");
        Assert.Equal(HttpStatusCode.OK, companiesResponse.StatusCode);
        var companies = await companiesResponse.Content.ReadFromJsonAsync<List<CompanyDto>>();
        Assert.NotNull(companies);
        var company = Assert.Single(companies.Where(c => c.Title == "ООО ДанныеАналитика").Take(1));

        var headPositionResponse = await _client.GetAsync($"companies/{company.CompanyId}/headPosition");
        Assert.Equal(HttpStatusCode.OK, headPositionResponse.StatusCode);
        var headPosition = await headPositionResponse.Content.ReadFromJsonAsync<PositionDto>();
        Assert.NotNull(headPosition);

        var subordinatesResponse = await _client.GetAsync($"positions/{headPosition.Id}/subordinates");
        Assert.Equal(HttpStatusCode.OK, subordinatesResponse.StatusCode);
        var subordinates = await subordinatesResponse.Content.ReadFromJsonAsync<List<PositionHierarchyDto>>();
        Assert.NotNull(subordinates);
        Assert.NotEmpty(subordinates);
        
        var currentEmployeesResponse = await _client.GetAsync($"employees/{company.CompanyId}/currentEmployees/");
        if (currentEmployeesResponse.StatusCode != HttpStatusCode.OK)
        {
            var body = await currentEmployeesResponse.Content.ReadAsStringAsync();
            var appLogs = await _fixture.GetApplicationLogsAsync();
            Assert.Fail(
                $"Current employees request failed. Status={(int)currentEmployeesResponse.StatusCode} {currentEmployeesResponse.StatusCode}. Body={body}{Environment.NewLine}App logs:{Environment.NewLine}{appLogs}");
        }
        var currentEmployees = await currentEmployeesResponse.Content.ReadFromJsonAsync<List<CurrentPositionEmployeeDto>>();
        Assert.NotNull(currentEmployees);
        Assert.NotEmpty(currentEmployees);

        var testEmployeePositionId = currentEmployees.Where(e => e.EmployeeId == authData.Id).Select(e => e.PositionId).FirstOrDefault();
        var subordinatePositionId = subordinates.Where(s => s.ParentId == testEmployeePositionId).Select(s => s.PositionId).FirstOrDefault();
        var subordinateWithPosition = currentEmployees.FirstOrDefault(e => e.PositionId == subordinatePositionId);
        Assert.NotNull(subordinateWithPosition);

        var createScoreRequest = new CreateScoreDto(
            subordinateWithPosition.EmployeeId,
            authData.Id,
            subordinateWithPosition.PositionId,
            DateTimeOffset.UtcNow.AddDays(-7),
            5,
            4,
            5);

        var createScoreResponse = await _client.PostAsJsonAsync("scores", createScoreRequest);
        Assert.Equal(HttpStatusCode.Created, createScoreResponse.StatusCode);
        var createdScore = await createScoreResponse.Content.ReadFromJsonAsync<ScoreDto>();
        Assert.NotNull(createdScore);

        var lastScoresResponse =
            await _client.GetAsync($"employees/{authData.Id}/subordinates/lasrScores");
        Assert.Equal(HttpStatusCode.OK, lastScoresResponse.StatusCode);
        var lastScores = await lastScoresResponse.Content.ReadFromJsonAsync<List<ScoreDto>>();
        Assert.NotNull(lastScores);
        Assert.NotEmpty(lastScores);

        var matched = lastScores.FirstOrDefault(s =>
            s.EmployeeId == subordinateWithPosition.EmployeeId &&
            s.PositionId == subordinateWithPosition.PositionId &&
            s.CreatedAt >= LastMonthBoundary);

        Assert.NotNull(matched);
        Assert.Equal(createdScore.Id, matched.Id);
    }

    private async Task<AuthorizationDataDto> RegisterAndAuthorizeAsync()
    {
        var registerResponse = await _client.PostAsJsonAsync(
            "auth/register",
            new LoginDto(SeedEmployeePassword, SeedEmployeeEmail));
        if (registerResponse.StatusCode != HttpStatusCode.OK)
        {
            var registerBody = await registerResponse.Content.ReadAsStringAsync();
            Assert.Fail($"Register failed. Status={(int)registerResponse.StatusCode} {registerResponse.StatusCode}. Body={registerBody}");
        }

        var loginResponse = await _client.PostAsJsonAsync(
            "auth/login",
            new LoginDto(SeedEmployeePassword, SeedEmployeeEmail));
        if (loginResponse.StatusCode != HttpStatusCode.OK)
        {
            var loginBody = await loginResponse.Content.ReadAsStringAsync();
            Assert.Fail($"Login failed. Status={(int)loginResponse.StatusCode} {loginResponse.StatusCode}. Body={loginBody}");
        }

        var authData = await loginResponse.Content.ReadFromJsonAsync<AuthorizationDataDto>();
        Assert.NotNull(authData);
        Assert.False(string.IsNullOrWhiteSpace(authData.Token));

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authData.Token);
        return authData;
    }
}

