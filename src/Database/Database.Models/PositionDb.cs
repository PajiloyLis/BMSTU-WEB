using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Project.Database.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Database.Models;

public class PositionDb
{
    public PositionDb()
    {
    }

    public PositionDb(Guid id, Guid? parentId, string title, Guid companyId)
    {
        if (!Guid.TryParse(id.ToString(), out _))
            throw new ArgumentException("Invalid Id format", nameof(id));
        if (parentId.HasValue && !Guid.TryParse(parentId.Value.ToString(), out _))
            throw new ArgumentException("Invalid ParentId format", nameof(parentId));
        if (!Guid.TryParse(companyId.ToString(), out _))
            throw new ArgumentException("Invalid CompanyId format", nameof(companyId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        Id = id;
        ParentId = parentId;
        Title = title;
        CompanyId = companyId;
        IsDeleted = false;
    }

    [Key] public Guid Id { get; set; }

    [Column("position_id")][ForeignKey(nameof(PositionDb))] public Guid? ParentId { get; set; }

    [Required] public string Title { get; set; } = null!;

    [Column("company_id")][ForeignKey(nameof(CompanyDb))] public Guid CompanyId { get; set; }
    
    [Required] public bool  IsDeleted { get; set; }

    // Навигационные свойства
    public ICollection<PositionDb> Children { get; set; } = new List<PositionDb>();

    public ICollection<ScoreDb> Scores { get; set; } = new List<ScoreDb>();

    public ICollection<PositionHistoryDb> PositionHistories { get; set; } = new List<PositionHistoryDb>();
}

public class PositionMongoDb
{
    public PositionMongoDb(Guid id, Guid? parentId, string title, Guid companyId)
    {
        if (!Guid.TryParse(id.ToString(), out _))
            throw new ArgumentException("Invalid Id format", nameof(id));
        if (parentId.HasValue && !Guid.TryParse(parentId.Value.ToString(), out _))
            throw new ArgumentException("Invalid ParentId format", nameof(parentId));
        if (!Guid.TryParse(companyId.ToString(), out _))
            throw new ArgumentException("Invalid CompanyId format", nameof(companyId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        Id = id;
        ParentId = parentId;
        Title = title;
        CompanyId = companyId;
        IsDeleted = false;
    }

    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    
    [BsonRepresentation(BsonType.String)]
    public Guid? ParentId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    [BsonRepresentation(BsonType.String)]
    public Guid CompanyId { get; set; }
    
    public bool IsDeleted { get; set; }
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; }
}