using System.Diagnostics.CodeAnalysis;
using Project.Core.Models.PositionHistory;
using Project.Dto.Http.PositionHistory;

namespace Project.Dto.Http.Converters;

public static class PositionHistoryConverter
{
    [return: NotNullIfNotNull(nameof(position))]
    public static PositionHistoryDto? Convert(BasePositionHistory? position)
    {
        if (position is null)
            return null;

        return new PositionHistoryDto(position.PositionId,
            position.EmployeeId,
            position.StartDate,
            position.EndDate
        );
    }

    public static CurrentPositionEmployeeDto? ReducedConvert(BasePositionHistory? position)
    {
        if (position is null)
            return null;

        return new CurrentPositionEmployeeDto(position.PositionId, position.EmployeeId);
    }
}