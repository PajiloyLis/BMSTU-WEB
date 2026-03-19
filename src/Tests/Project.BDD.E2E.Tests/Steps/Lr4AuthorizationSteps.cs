using System.Net;
using System.Net.Http.Json;
using Project.BDD.E2E.Tests.Infrastructure;
using Project.Dto.Http;
using Project.Dto.Http.User;
using Reqnroll;
using Xunit;

namespace Project.BDD.E2E.Tests.Steps;

[Binding]
public sealed class Lr4AuthorizationSteps
{
    private readonly BddScenarioState _state;

    public Lr4AuthorizationSteps(ScenarioContext scenarioContext)
    {
        _state = scenarioContext[DatabaseHooks.ScenarioStateKey] as BddScenarioState
                 ?? throw new InvalidOperationException("Scenario state is not initialized.");
    }

    [When("пользователь начинает вход с email {string} и паролем {string}")]
    public async Task WhenUserStartsLoginAsync(string email, string password)
    {
        _state.LastResponse?.Dispose();
        _state.LastLoginStartResponse = null;
        _state.LastResponse = await _state.Client!.PostAsJsonAsync(
            "api/v1/auth/login/start",
            new LoginDto(password, email));

        if (_state.LastResponse.StatusCode != HttpStatusCode.OK)
            return;

        var startPayload = await _state.LastResponse.Content.ReadFromJsonAsync<LoginStartResponseDto>();
        _state.LastLoginStartResponse = startPayload;
        _state.OtpChallengeId = startPayload?.ChallengeId;
        _state.LastOtpCode = startPayload?.OtpCodeForTests;
        if (startPayload?.AuthorizationData is not null)
        {
            _state.AuthData = startPayload.AuthorizationData;
            _state.SetBearerTokenIfPresent();
        }
    }

    [Then("система сообщает что требуется OTP")]
    public async Task ThenSystemRequiresOtpAsync()
    {
        Assert.NotNull(_state.LastResponse);
        Assert.Equal(HttpStatusCode.OK, _state.LastResponse!.StatusCode);

        var payload = _state.LastLoginStartResponse;
        Assert.NotNull(payload);
        Assert.True(payload.RequiresOtp);
        Assert.NotNull(payload.ChallengeId);
    }

    [Given("пользователь начал вход и получил OTP challenge")]
    public async Task GivenUserStartedLoginAndReceivedOtpChallengeAsync()
    {
        Assert.False(string.IsNullOrWhiteSpace(_state.RegisteredEmail));
        Assert.False(string.IsNullOrWhiteSpace(_state.RegisteredPassword));
        await WhenUserStartsLoginAsync(_state.RegisteredEmail!, _state.RegisteredPassword!);
        await ThenSystemRequiresOtpAsync();
    }

    [When("пользователь подтверждает вход корректным OTP")]
    public async Task WhenUserCompletesLoginWithValidOtpAsync()
    {
        Assert.NotNull(_state.OtpChallengeId);
        Assert.False(string.IsNullOrWhiteSpace(_state.LastOtpCode));

        _state.LastResponse?.Dispose();
        _state.LastResponse = await _state.Client!.PostAsJsonAsync(
            "api/v1/auth/login/complete",
            new LoginOtpConfirmDto(_state.OtpChallengeId!.Value, _state.LastOtpCode!));
    }

    [When("пользователь {int} раз вводит неверный пароль")]
    public async Task WhenUserEntersInvalidPasswordSeveralTimesAsync(int attempts)
    {
        Assert.False(string.IsNullOrWhiteSpace(_state.RegisteredEmail));
        for (var i = 0; i < attempts; i++)
        {
            _state.LastResponse?.Dispose();
            _state.LastResponse = await _state.Client!.PostAsJsonAsync(
                "api/v1/auth/login/start",
                new LoginDto("wrong-password", _state.RegisteredEmail!));
        }
    }

    [Then("учетная запись переходит в состояние блокировки")]
    public async Task ThenAccountIsLockedAsync()
    {
        Assert.False(string.IsNullOrWhiteSpace(_state.RegisteredEmail));
        Assert.False(string.IsNullOrWhiteSpace(_state.RegisteredPassword));

        _state.LastResponse?.Dispose();
        _state.LastResponse = await _state.Client!.PostAsJsonAsync(
            "api/v1/auth/login/start",
            new LoginDto(_state.RegisteredPassword!, _state.RegisteredEmail!));

        Assert.Equal((HttpStatusCode)423, _state.LastResponse.StatusCode);
    }

    [Then("очередная попытка входа отклоняется с кодом {int}")]
    public void ThenNextLoginAttemptHasStatusCode(int statusCode)
    {
        Assert.NotNull(_state.LastResponse);
        Assert.Equal((HttpStatusCode)statusCode, _state.LastResponse!.StatusCode);
    }

    [Given("учетная запись пользователя заблокирована")]
    public async Task GivenUserAccountLockedAsync()
    {
        await WhenUserEntersInvalidPasswordSeveralTimesAsync(5);
        await ThenAccountIsLockedAsync();
    }

    [When("истекает период блокировки")]
    public async Task WhenLockoutPeriodExpiresAsync()
    {
        Assert.False(string.IsNullOrWhiteSpace(_state.RegisteredEmail));
        await BddEnvironmentHost.Instance.ForceLockoutExpiredAsync(_state.RegisteredEmail!);
    }

    [When("пользователь запрашивает восстановление пароля для {string}")]
    public async Task WhenUserRequestsPasswordRecoveryAsync(string email)
    {
        _state.LastResponse?.Dispose();
        _state.LastPasswordRecoveryResponse = null;
        _state.LastResponse = await _state.Client!.PostAsJsonAsync(
            "api/v1/auth/password/recovery/request",
            new PasswordRecoveryRequestDto(email));

        if (_state.LastResponse.StatusCode != HttpStatusCode.Accepted)
            return;

        var payload = await _state.LastResponse.Content.ReadFromJsonAsync<PasswordRecoveryRequestResponseDto>();
        _state.LastPasswordRecoveryResponse = payload;
        _state.RecoveryToken = payload?.RecoveryTokenForTests;
    }

    [Then("отправлен recovery токен")]
    public async Task ThenRecoveryTokenIsReturnedAsync()
    {
        Assert.NotNull(_state.LastResponse);
        Assert.Equal(HttpStatusCode.Accepted, _state.LastResponse!.StatusCode);
        var payload = _state.LastPasswordRecoveryResponse;
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.RecoveryTokenForTests));
        _state.RecoveryToken = payload.RecoveryTokenForTests;
    }

    [Given("для пользователя выпущен recovery токен")]
    public async Task GivenRecoveryTokenIssuedAsync()
    {
        Assert.False(string.IsNullOrWhiteSpace(_state.RegisteredEmail));
        await WhenUserRequestsPasswordRecoveryAsync(_state.RegisteredEmail!);
        await ThenRecoveryTokenIsReturnedAsync();
    }

    [When("пользователь задает новый пароль через recovery токен")]
    public async Task WhenUserSetsNewPasswordViaRecoveryTokenAsync()
    {
        Assert.False(string.IsNullOrWhiteSpace(_state.RegisteredEmail));
        Assert.False(string.IsNullOrWhiteSpace(_state.RecoveryToken));
        Assert.False(string.IsNullOrWhiteSpace(_state.RegisteredPassword));

        _state.NewPassword = $"{_state.RegisteredPassword}-new";
        _state.LastResponse?.Dispose();
        _state.LastResponse = await _state.Client!.PostAsJsonAsync(
            "api/v1/auth/password/recovery/confirm",
            new PasswordRecoveryConfirmDto(_state.RegisteredEmail!, _state.RecoveryToken!, _state.NewPassword));
    }

    [Then("вход со старым паролем недоступен")]
    public async Task ThenLoginWithOldPasswordIsUnavailableAsync()
    {
        Assert.False(string.IsNullOrWhiteSpace(_state.RegisteredEmail));
        Assert.False(string.IsNullOrWhiteSpace(_state.RegisteredPassword));

        var response = await _state.Client!.PostAsJsonAsync(
            "api/v1/auth/login/start",
            new LoginDto(_state.RegisteredPassword!, _state.RegisteredEmail!));

        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized);
    }

    [Then("вход с новым паролем доступен")]
    public async Task ThenLoginWithNewPasswordIsAvailableAsync()
    {
        Assert.False(string.IsNullOrWhiteSpace(_state.RegisteredEmail));
        Assert.False(string.IsNullOrWhiteSpace(_state.NewPassword));

        var startResponse = await _state.Client!.PostAsJsonAsync(
            "api/v1/auth/login/start",
            new LoginDto(_state.NewPassword!, _state.RegisteredEmail!));
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        var payload = await startResponse.Content.ReadFromJsonAsync<LoginStartResponseDto>();
        Assert.NotNull(payload);
        if (payload.RequiresOtp)
        {
            Assert.NotNull(payload.ChallengeId);
            Assert.False(string.IsNullOrWhiteSpace(payload.OtpCodeForTests));
            var completeResponse = await _state.Client.PostAsJsonAsync(
                "api/v1/auth/login/complete",
                new LoginOtpConfirmDto(payload.ChallengeId!.Value, payload.OtpCodeForTests!));
            Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        }
        else
        {
            Assert.NotNull(payload.AuthorizationData);
        }
    }

    [Given("для пользователя истек срок действия пароля")]
    public async Task GivenPasswordExpiredForUserAsync()
    {
        Assert.False(string.IsNullOrWhiteSpace(_state.RegisteredEmail));
        await BddEnvironmentHost.Instance.ForcePasswordExpiredAsync(_state.RegisteredEmail!);
    }

    [Then("система сообщает что требуется смена пароля")]
    public async Task ThenSystemReportsPasswordChangeRequiredAsync()
    {
        Assert.NotNull(_state.LastResponse);
        var payload = await _state.LastResponse!.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(payload);
        Assert.Equal("PasswordChangeRequiredException", payload.ErrorType);
    }

    [When("пользователь меняет пароль на {string}")]
    public async Task WhenUserChangesPasswordAsync(string newPassword)
    {
        Assert.False(string.IsNullOrWhiteSpace(_state.RegisteredEmail));
        Assert.False(string.IsNullOrWhiteSpace(_state.RegisteredPassword));

        _state.NewPassword = newPassword;
        _state.LastResponse?.Dispose();
        _state.LastResponse = await _state.Client!.PostAsJsonAsync(
            "api/v1/auth/password/change",
            new PasswordChangeDto(_state.RegisteredEmail!, _state.RegisteredPassword!, newPassword));

        if (_state.LastResponse.StatusCode == HttpStatusCode.OK)
            _state.RegisteredPassword = newPassword;
    }
}
