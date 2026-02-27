using Project.Dto.Http.PositionHistory;

namespace Project.Integration.Tests.Factories;

public static class PositionHistoryObjectFabric
{
    public static CreatePositionHistoryDto CreatePositionHistoryDto(
        Guid positionId,
        Guid employeeId,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        return new CreatePositionHistoryDto(
            positionId,
            employeeId,
            startDate ?? new DateOnly(2019, 1, 1),
            endDate ?? new DateOnly(2020, 1, 1));
    }

    public static UpdatePositionHistoryDto UpdatePositionHistoryDto(
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        return new UpdatePositionHistoryDto(
            startDate ?? new DateOnly(2018, 1, 1),
            endDate ?? new DateOnly(2019, 1, 1));
    }
}

