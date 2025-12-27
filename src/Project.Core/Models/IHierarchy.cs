namespace Project.Core.Models;

public interface IHierarchy
{
    public Guid? ParentId { get; set; }
    public Guid OwnId { get; set; }    
}