using System.ComponentModel.DataAnnotations.Schema;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Database.Models;

public class PositionHierarchyWithEmployeeIdDb
{
        public PositionHierarchyWithEmployeeIdDb(Guid employeeId, Guid positionId, Guid? parentId, string title, int level)
        {
                EmployeeId = employeeId;
                PositionId = positionId;
                ParentId = parentId;
                Title = title;
                Level = level;
        }
        [Column("employee_id")]
        public Guid EmployeeId { get; set; }
        [Column("position_id")]
        public Guid PositionId { get; set; }
        [Column("parent_id")]
        public Guid? ParentId { get; set; }
        [Column("title")]
        public string Title { get; set; }
        [Column("level")]
        public int Level { get; set; }
}

public class PositionHierarchyWithEmployeeIdMongoDb
{
    public PositionHierarchyWithEmployeeIdMongoDb(Guid employeeId, Guid positionId, Guid? parentId, string title, int level)
    {
        EmployeeId = employeeId;
        PositionId = positionId;
        ParentId = parentId;
        Title = title;
        Level = level;
    }
    
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    
    [BsonRepresentation(BsonType.String)]
    public Guid EmployeeId { get; set; }
    
    [BsonRepresentation(BsonType.String)]
    public Guid PositionId { get; set; }
    
    [BsonRepresentation(BsonType.String)]
    public Guid? ParentId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public int Level { get; set; }
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; }
}