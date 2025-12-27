namespace Project.Dto.Http.Position;

public class PositionDto
{
    public PositionDto(Guid id, Guid? parentId, string title, Guid companyId, bool isDeleted)
    {
        Id = id;
        ParentId = parentId;
        Title = title;
        CompanyId = companyId;
        IsDeleted = isDeleted;
    }

    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public string Title { get; init; }
    public Guid CompanyId { get; init; }
    
    public bool IsDeleted { get; init; }
}