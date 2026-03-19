using System.Net.Http.Headers;
using Project.Dto.Http.User;

namespace Project.BDD.E2E.Tests.Infrastructure;

internal sealed class BddScenarioState
{
    public HttpClient? Client { get; set; }
    public HttpResponseMessage? LastResponse { get; set; }
    public AuthorizationDataDto? AuthData { get; set; }
    public LoginStartResponseDto? LastLoginStartResponse { get; set; }
    public PasswordRecoveryRequestResponseDto? LastPasswordRecoveryResponse { get; set; }
    public string? RegisteredEmail { get; set; }
    public string? RegisteredPassword { get; set; }
    public string? NewPassword { get; set; }
    public Guid? OtpChallengeId { get; set; }
    public string? LastOtpCode { get; set; }
    public string? RecoveryToken { get; set; }

    public void SetBearerTokenIfPresent()
    {
        if (Client is null || AuthData is null || string.IsNullOrWhiteSpace(AuthData.Token))
            return;

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthData.Token);
    }
}
