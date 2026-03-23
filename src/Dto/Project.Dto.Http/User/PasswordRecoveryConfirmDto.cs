namespace Project.Dto.Http.User;

public sealed class PasswordRecoveryConfirmDto
{
    public PasswordRecoveryConfirmDto(string email, string recoveryToken, string newPassword)
    {
        Email = email;
        RecoveryToken = recoveryToken;
        NewPassword = newPassword;
    }

    public string Email { get; set; }

    public string RecoveryToken { get; set; }

    public string NewPassword { get; set; }
}
