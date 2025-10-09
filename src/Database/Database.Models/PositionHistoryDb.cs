using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Database.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Project.Database.Models;

public class PositionHistoryDb
{
    public PositionHistoryDb(Guid positionId, Guid employeeId, DateOnly startDate, DateOnly? endDate = null)
    {
        PositionId = positionId;
        EmployeeId = employeeId;
        StartDate = startDate;
        EndDate = endDate;
    }

    [Column("position_id")][ForeignKey(nameof(PositionDb))]
    public Guid PositionId { get; set; }
    [Column("employee_id")][ForeignKey(nameof(EmployeeDb))]
    public Guid EmployeeId { get; set; }
    [Required]
    public DateOnly StartDate { get; set; }
    [Required]
    public DateOnly? EndDate { get; set; }
}

public class PositionHistoryMongoDb
{
    public PositionHistoryMongoDb(Guid positionId, Guid employeeId, DateTime startDate, DateTime? endDate = null)
    {
        PositionId = positionId;
        EmployeeId = employeeId;
        StartDate = startDate;
        EndDate = endDate;
    }

    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    
    [BsonRepresentation(BsonType.String)]
    public Guid PositionId { get; set; }
    
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