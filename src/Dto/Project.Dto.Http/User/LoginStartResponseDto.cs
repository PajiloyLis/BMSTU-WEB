namespace Project.Dto.Http.User;

public sealed class LoginStartResponseDto
{
    public LoginStartResponseDto(
        bool requiresOtp,
        Guid? challengeId,
        DateTimeOffset? otpExpiresAtUtc,
        AuthorizationDataDto? authorizationData,
        string? otpCodeForTests = null)
    {
        RequiresOtp = requiresOtp;
        ChallengeId = challengeId;
        OtpExpiresAtUtc = otpExpiresAtUtc;
        AuthorizationData = authorizationData;
        OtpCodeForTests = otpCodeForTests;
    }

    public bool RequiresOtp { get; set; }

    public Guid? ChallengeId { get; set; }

    public DateTimeOffset? OtpExpiresAtUtc { get; set; }

    public AuthorizationDataDto? AuthorizationData { get; set; }

    public string? OtpCodeForTests { get; set; }
}
