namespace Project.Core.Models.User;

public sealed class LoginStartResult
{
    public LoginStartResult(
        bool requiresOtp,
        Guid? challengeId,
        DateTimeOffset? otpExpiresAtUtc,
        AuthorizationData? authorizationData,
        string? otpCodeForTests = null)
    {
        RequiresOtp = requiresOtp;
        ChallengeId = challengeId;
        OtpExpiresAtUtc = otpExpiresAtUtc;
        AuthorizationData = authorizationData;
        OtpCodeForTests = otpCodeForTests;
    }

    public bool RequiresOtp { get; }

    public Guid? ChallengeId { get; }

    public DateTimeOffset? OtpExpiresAtUtc { get; }

    public AuthorizationData? AuthorizationData { get; }

    public string? OtpCodeForTests { get; }
}
