using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Database.Models;

public class PostHistoryDb
{
    public PostHistoryDb()
    {
    }

    public PostHistoryDb(
        Guid postId,
        Guid employeeId,
        DateOnly startDate,
        DateOnly? endDate = null)
    {
        PostId = postId;
        EmployeeId = employeeId;
        StartDate = startDate;
        EndDate = endDate;
    }
    [Column("post_id")]
    [ForeignKey(nameof(PostDb))]
    public Guid PostId { get; set; }
    
    [Column("employee_id")]
    [ForeignKey(nameof(EmployeeDb))]
    public Guid EmployeeId { get; set; }
    [Required]
    public DateOnly StartDate { get; set; }
    [Required]
    public DateOnly? EndDate { get; set; }
}

public class PostHistoryMongoDb
{
    public PostHistoryMongoDb(
        Guid postId,
        Guid employeeId,
        DateTime startDate,
        DateTime? endDate = null)
    {
        PostId = postId;
        EmployeeId = employeeId;
        StartDate = startDate;
        EndDate = endDate;
    }
    
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    
    [BsonRepresentation(BsonType.String)]
    public Guid PostId { get; set; }
    
    [BsonRepresentation(BsonType.String)]
    public Guid EmployeeId { get; set; }
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime StartDate { get; set; }
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? EndDate { get; set; }
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; }
}