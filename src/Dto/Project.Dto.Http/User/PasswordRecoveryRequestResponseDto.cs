namespace Project.Dto.Http.User;

public sealed class PasswordRecoveryRequestResponseDto
{
    public PasswordRecoveryRequestResponseDto(string message, string? recoveryTokenForTests = null)
    {
        Message = message;
        RecoveryTokenForTests = recoveryTokenForTests;
    }

    public string Message { get; set; }

    public string? RecoveryTokenForTests { get; set; }
}
