using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Database.Models;

/// <summary>
/// Database post model.
/// </summary>
public class PostDb
{
    public PostDb()
    {
        Title = string.Empty;
        PostHistories = new List<PostHistoryDb>();
        IsDeleted = false;
    }
    
    public PostDb(Guid id, string title, decimal salary, Guid companyId, bool isDeleted = false)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty");

        if (salary <= 0)
            throw new ArgumentException("Salary must be greater than zero");

        Id = id;
        Title = title;
        Salary = salary;
        CompanyId = companyId;
        PostHistories = new List<PostHistoryDb>();
        IsDeleted = isDeleted;
    }

    /// <summary>
    /// Post id.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Post title.
    /// </summary>
    [Required]
    public string Title { get; set; }

    /// <summary>
    /// Post salary.
    /// </summary>
    [Required]
    public decimal Salary { get; set; }

    /// <summary>
    /// Company id.
    /// </summary>
    [Column("company_id")][ForeignKey(nameof(CompanyDb))]
    public Guid CompanyId { get; set; }
    
    [Required]
    public bool IsDeleted { get; set; }

    public ICollection<PostHistoryDb> PostHistories { get; set; }
}

public class PostMongoDb
{
    public PostMongoDb(Guid id, string title, decimal salary, Guid companyId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty");

        if (salary <= 0)
            throw new ArgumentException("Salary must be greater than zero");

        Id = id;
        Title = title;
        Salary = salary;
        CompanyId = companyId;
        IsDeleted = false;
    }

    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    
    [BsonRepresentation(BsonType.String)]
    public Guid CompanyId { get; set; }
    
    public bool IsDeleted { get; set; }
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; }
}