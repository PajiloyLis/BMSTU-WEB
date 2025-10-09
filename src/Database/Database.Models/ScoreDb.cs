using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Database.Models;

/// <summary>
/// Database score story model.
/// </summary>
public class ScoreDb
{
    public ScoreDb(
        Guid id,
        Guid employeeId,
        Guid authorId,
        Guid positionId,
        DateTimeOffset createdAt,
        int efficiencyScore,
        int engagementScore,
        int competencyScore)
    {
        if (efficiencyScore <= 0 || efficiencyScore >= 6)
            throw new ArgumentException("EfficiencyScore must be between 1 and 5", nameof(efficiencyScore));

        if (engagementScore <= 0 || engagementScore >= 6)
            throw new ArgumentException("EngagementScore must be between 1 and 5", nameof(engagementScore));

        if (competencyScore <= 0 || competencyScore >= 6)
            throw new ArgumentException("CompetencyScore must be between 1 and 5", nameof(competencyScore));

        Id = id;
        EmployeeId = employeeId;
        AuthorId = authorId;
        PositionId = positionId;
        CreatedAt = createdAt;
        EfficiencyScore = efficiencyScore;
        EngagementScore = engagementScore;
        CompetencyScore = competencyScore;
    }

    /// <summary>
    /// Score id.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Employee id.
    /// </summary>
    [Column("employee_id")][ForeignKey(nameof(EmployeeDb))]
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Author id.
    /// </summary>
    [Column("author_id")][ForeignKey(nameof(EmployeeDb))]
    public Guid AuthorId { get; set; }

    /// <summary>
    /// Position id.
    /// </summary>
    [Column("position_id")][ForeignKey(nameof(PositionDb))]
    public Guid PositionId { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Efficiency score (1-5).
    /// </summary>
    [Required]
    public int EfficiencyScore { get; set; }

    /// <summary>
    /// Engagement score (1-5).
    /// </summary>
    [Required]
    public int EngagementScore { get; set; }

    /// <summary>
    /// Competency score (1-5).
    /// </summary>
    [Required]
    public int CompetencyScore { get; set; }
}

public class ScoreMongoDb
{
    public ScoreMongoDb(
        Guid id,
        Guid employeeId,
        Guid authorId,
        Guid positionId,
        DateTimeOffset createdAt,
        int efficiencyScore,
        int engagementScore,
        int competencyScore)
    {
        if (efficiencyScore <= 0 || efficiencyScore >= 6)
            throw new ArgumentException("EfficiencyScore must be between 1 and 5", nameof(efficiencyScore));

        if (engagementScore <= 0 || engagementScore >= 6)
            throw new ArgumentException("EngagementScore must be between 1 and 5", nameof(engagementScore));

        if (competencyScore <= 0 || competencyScore >= 6)
            throw new ArgumentException("CompetencyScore must be between 1 and 5", nameof(competencyScore));

        Id = id;
        EmployeeId = employeeId;
        AuthorId = authorId;
        PositionId = positionId;
        CreatedAt = createdAt;
        EfficiencyScore = efficiencyScore;
        EngagementScore = engagementScore;
        CompetencyScore = competencyScore;
    }

    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    
    [BsonRepresentation(BsonType.String)]
    public Guid EmployeeId { get; set; }
    
    [BsonRepresentation(BsonType.String)]
    public Guid AuthorId { get; set; }
    
    [BsonRepresentation(BsonType.String)]
    public Guid PositionId { get; set; }
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTimeOffset CreatedAt { get; set; }
    
    public int EfficiencyScore { get; set; }
    public int EngagementScore { get; set; }
    public int CompetencyScore { get; set; }
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; }
}