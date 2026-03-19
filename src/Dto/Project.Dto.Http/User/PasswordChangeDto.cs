namespace Project.Dto.Http.User;

public sealed class PasswordChangeDto
{
    public PasswordChangeDto(string email, string oldPassword, string newPassword)
    {
        Email = email;
        OldPassword = oldPassword;
        NewPassword = newPassword;
    }

    public string Email { get; set; }

    public string OldPassword { get; set; }

    public string NewPassword { get; set; }
}
