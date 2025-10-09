using System.ComponentModel.DataAnnotations;
using Project.Database.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Database.Models;

/// <summary>
/// Database employee model.
/// </summary>
public class EmployeeDb
{
    public EmployeeDb(Guid id,
        string fullName,
        string phone,
        string email,
        DateOnly birthDate,
        string? photo,
        string? duties)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("FullName cannot be empty");

        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone cannot be empty");

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty");

        if (birthDate > DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("BirthDate cannot be later than today");

        Id = id;
        FullName = fullName;
        Phone = phone;
        Email = email;
        BirthDate = birthDate;
        Photo = photo;
        Duties = duties;
    }

    /// <summary>
    /// Employee id.
    /// </summary>
    [Key]
    public Guid Id { get; init; }

    /// <summary>
    /// Employee full name.
    /// </summary>
    [Required]
    public string FullName { get; set; }

    /// <summary>
    /// Employee business phone number.
    /// </summary>
    [Required]
    public string Phone { get; set; }

    /// <summary>
    /// Employee business email.
    /// </summary>
    [Required]
    public string Email { get; set; }

    /// <summary>
    /// Employee birthday.
    /// </summary>
    [Required]
    public DateOnly BirthDate { get; set; }

    /// <summary>
    /// Employee photo path.
    /// </summary>
    public string? Photo { get; set; }

    /// <summary>
    /// Employee duties json formated
    /// </summary>
    public string? Duties { get; set; }

    public ICollection<EducationDb> Educations { get; set; } = new List<EducationDb>();

    public ICollection<ScoreDb> Scores { get; set; } = new List<ScoreDb>();
    
    public ICollection<ScoreDb> AuthoredScores { get; set; } = new List<ScoreDb>();

    public ICollection<PostHistoryDb> PostHistories { get; set; } = new List<PostHistoryDb>();

    public ICollection<PositionHistoryDb> PositionHistories { get; set; } = new List<PositionHistoryDb>();
}

public class EmployeeMongoDb
{
    public EmployeeMongoDb(Guid id,
        string fullName,
        string phone,
        string email,
        DateTime birthDate,
        string? photo,
        string? duties)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("FullName cannot be empty");

        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone cannot be empty");

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty");

        if (birthDate > DateTime.Today)
            throw new ArgumentException("BirthDate cannot be later than today");

        Id = id;
        FullName = fullName;
        Phone = phone;
        Email = email;
        BirthDate = birthDate;
        Photo = photo;
        Duties = duties;
    }

    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime BirthDate { get; set; }
    
    public string? Photo { get; set; }
    public string? Duties { get; set; }
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; }
}