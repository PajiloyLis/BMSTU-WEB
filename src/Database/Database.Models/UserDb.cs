using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Database.Models;

public class UserDb
{
    public UserDb(string email, string password, string salt, string role, Guid id)
    {
        Email = email;
        Password = password;
        Salt = salt;
        Role = role;
        Id = id;
    }
    public string Password { get; set; }

    public string Email { get; set; }
    
    public string Salt { get; set; }
    
    public string Role { get; set; }
    
    public Guid Id { get; set; }
}

public class UserMongoDb
{
    public UserMongoDb(string email, string password, string salt, string role)
    {
        Email = email;
        Password = password;
        Salt = salt;
        Role = role;
    }
    
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; }
}