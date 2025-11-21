using System.Text.RegularExpressions;

namespace Project.Core.Models.User;

public class AuthorizationData
{
    public AuthorizationData(string email, string token, Guid id)
    {
        if (!Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$") || email.Length > 254)
            throw new ArgumentException("Invalid employee email", nameof(email));
        if (!Guid.TryParse(id.ToString(), out _))
            throw new ArgumentException("User id is invalid");
        Email = email;
        Token = token;
        Id = id;
    }

    public string Token { get; set; }

    public string Email { get; set; }
    
    public Guid Id { get; set; }
}