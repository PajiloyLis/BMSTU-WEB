namespace Project.Dto.Http.User;

public sealed class LoginOtpConfirmDto
{
    public LoginOtpConfirmDto(Guid challengeId, string otpCode)
    {
        ChallengeId = challengeId;
        OtpCode = otpCode;
    }

    public Guid ChallengeId { get; set; }

    public string OtpCode { get; set; }
}
