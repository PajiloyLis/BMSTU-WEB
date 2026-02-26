using Project.Core.Models.PositionHistory;
using Project.Dto.Http.PositionHistory;

namespace Project.Controller.Tests.Factories;

public static class PositionHistoryObjectFabric
{
    public static CreatePositionHistoryDto CreatePositionHistoryDto(Guid positionId, Guid employeeId)
    {
        return new CreatePositionHistoryDto(
            positionId,
            employeeId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
    }

    public static UpdatePositionHistoryDto UpdatePositionHistoryDto()
    {
        return new UpdatePositionHistoryDto(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)));
    }

    public static BasePositionHistory BasePositionHistory(Guid positionId, Guid employeeId)
    {
        return new BasePositionHistory(
            positionId,
            employeeId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
    }

    public static PositionHierarchyWithEmployee PositionHierarchyWithEmployee(Guid employeeId, Guid positionId, string title = "Subordinate")
    {
        return new PositionHierarchyWithEmployee(employeeId, positionId, Guid.Empty, title, 1);
    }
}

