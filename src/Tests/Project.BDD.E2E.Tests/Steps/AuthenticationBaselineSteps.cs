using System.Net;
using System.Net.Http.Json;
using Project.Dto.Http;
using Project.Dto.Http.User;
using Project.BDD.E2E.Tests.Infrastructure;
using Reqnroll;
using Xunit;

namespace Project.BDD.E2E.Tests.Steps;

[Binding]
public sealed class AuthenticationBaselineSteps
{
    private readonly BddScenarioState _state;

    public AuthenticationBaselineSteps(ScenarioContext scenarioContext)
    {
        _state = scenarioContext[DatabaseHooks.ScenarioStateKey] as BddScenarioState
                 ?? throw new InvalidOperationException("Scenario state is not initialized.");
    }

    [Given("открыт API приложения")]
    public void GivenApiIsAvailable()
    {
        Assert.NotNull(_state.Client);
    }

    [Given("зарегистрирован пользователь {string} с паролем {string}")]
    public async Task GivenRegisteredUserAsync(string email, string password)
    {
        _state.RegisteredEmail = email;
        _state.RegisteredPassword = password;
        _state.NewPassword = null;
        _state.OtpChallengeId = null;
        _state.LastOtpCode = null;
        _state.RecoveryToken = null;
        _state.LastLoginStartResponse = null;
        _state.LastPasswordRecoveryResponse = null;

        var response = await _state.Client!.PostAsJsonAsync(
            "api/v1/auth/register",
            new LoginDto(password, email));

        if (response.StatusCode is not HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Не удалось зарегистрировать пользователя. Status={(int)response.StatusCode} {response.StatusCode}. Body={body}");
        }
    }

    [When("пользователь логинится с email {string} и паролем {string}")]
    public async Task WhenUserLogsInAsync(string email, string password)
    {
        _state.LastResponse?.Dispose();
        _state.LastLoginStartResponse = null;
        var loginStartResponse = await _state.Client!.PostAsJsonAsync(
            "api/v1/auth/login/start",
            new LoginDto(password, email));

        if (loginStartResponse.StatusCode != HttpStatusCode.OK)
        {
            _state.LastResponse = loginStartResponse;
            return;
        }

        var startPayload = await loginStartResponse.Content.ReadFromJsonAsync<LoginStartResponseDto>();
        _state.LastLoginStartResponse = startPayload;
        if (startPayload is null || !startPayload.RequiresOtp)
        {
            _state.LastResponse = loginStartResponse;
            if (startPayload?.AuthorizationData is not null)
            {
                _state.AuthData = startPayload.AuthorizationData;
                _state.SetBearerTokenIfPresent();
            }

            return;
        }

        _state.OtpChallengeId = startPayload.ChallengeId;
        _state.LastOtpCode = startPayload.OtpCodeForTests;

        if (_state.OtpChallengeId is null || string.IsNullOrWhiteSpace(_state.LastOtpCode))
        {
            _state.LastResponse = loginStartResponse;
            return;
        }

        loginStartResponse.Dispose();
        _state.LastResponse = await _state.Client.PostAsJsonAsync(
            "api/v1/auth/login/complete",
            new LoginOtpConfirmDto(_state.OtpChallengeId.Value, _state.LastOtpCode));
    }

    [Then("код ответа равен {int}")]
    public void ThenStatusCodeIs(int statusCode)
    {
        Assert.NotNull(_state.LastResponse);
        Assert.Equal((HttpStatusCode)statusCode, _state.LastResponse!.StatusCode);
    }

    [Then("возвращается JWT токен")]
    public async Task ThenJwtIsReturnedAsync()
    {
        if (_state.AuthData is not null && !string.IsNullOrWhiteSpace(_state.AuthData.Token))
            return;

        Assert.NotNull(_state.LastResponse);
        var payload = await _state.LastResponse!.Content.ReadFromJsonAsync<AuthorizationDataDto>();
        if (payload is null || string.IsNullOrWhiteSpace(payload.Token))
        {
            var startPayload = _state.LastLoginStartResponse;
            payload = startPayload?.AuthorizationData;
        }

        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Token));

        _state.AuthData = payload!;
        _state.SetBearerTokenIfPresent();
    }

    [Then("возвращается ошибка типа {string}")]
    public async Task ThenErrorTypeIsAsync(string expectedType)
    {
        Assert.NotNull(_state.LastResponse);
        var payload = await _state.LastResponse!.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(payload);
        Assert.Equal(expectedType, payload.ErrorType);
    }
}
