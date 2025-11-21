namespace Project.Dto.Http.User;

public class AuthorizationDataDto
{
    public AuthorizationDataDto(string email, string token, Guid id)
    {
        Email = email;
        Token = token;
        Id = id;
    }

    public string Token { get; set; }

    public string Email { get; set; }
    
    public Guid Id { get; set; }
}