using Project.Core.Models.Position;
using Project.Dto.Http.Position;

namespace Project.Controller.Tests.Factories;

public static class PositionObjectFabric
{
    public static CreatePositionDto CreatePositionDto(Guid companyId, Guid? parentId = null, string title = "Developer")
    {
        return new CreatePositionDto(parentId, title, companyId);
    }

    public static BasePosition BasePosition(Guid positionId, Guid companyId, string title = "Developer", Guid? parentId = null)
    {
        return new BasePosition(positionId, parentId ?? Guid.Empty, title, companyId, false);
    }

    public static PositionHierarchy PositionHierarchy(Guid positionId, string title = "Subordinate", Guid? parentId = null, int level = 1)
    {
        return new PositionHierarchy(positionId, parentId ?? Guid.Empty, title, level);
    }
}

