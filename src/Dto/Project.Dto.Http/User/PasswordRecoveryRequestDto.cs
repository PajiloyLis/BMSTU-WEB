namespace Project.Dto.Http.User;

public sealed class PasswordRecoveryRequestDto
{
    public PasswordRecoveryRequestDto(string email)
    {
        Email = email;
    }

    public string Email { get; set; }
}
