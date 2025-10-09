using System.Diagnostics.CodeAnalysis;
using Project.Core.Models.PositionHistory;

namespace Project.Database.Models.Converters;

public static class PositionHistoryConverter
{
    [return: NotNullIfNotNull("positionHistory")]
    public static PositionHistoryDb? Convert(CreatePositionHistory? positionHistory)
    {
        if (positionHistory == null)
            return null;

        return new PositionHistoryDb(
            positionHistory.PositionId,
            positionHistory.EmployeeId,
            positionHistory.StartDate,
            positionHistory.EndDate);
    }

    [return: NotNullIfNotNull("positionHistory")]
    public static PositionHistoryDb? Convert(BasePositionHistory? positionHistory)
    {
        if (positionHistory == null)
            return null;

        return new PositionHistoryDb(
            positionHistory.PositionId,
            positionHistory.EmployeeId,
            positionHistory.StartDate,
            positionHistory.EndDate);
    }

    [return: NotNullIfNotNull("positionHistoryDb")]
    public static BasePositionHistory? Convert(PositionHistoryDb? positionHistoryDb)
    {
        if (positionHistoryDb == null)
            return null;

        return new BasePositionHistory(
            positionHistoryDb.PositionId,
            positionHistoryDb.EmployeeId,
            positionHistoryDb.StartDate,
            positionHistoryDb.EndDate);
    }

    [return: NotNullIfNotNull("positionHistory")]
    public static PositionHistoryMongoDb? ConvertMongo(CreatePositionHistory? positionHistory)
    {
        if (positionHistory == null)
            return null;

        return new PositionHistoryMongoDb(
            positionHistory.PositionId,
            positionHistory.EmployeeId,
            positionHistory.StartDate.ToDateTime(TimeOnly.MinValue),
            positionHistory.EndDate?.ToDateTime(TimeOnly.MinValue));
    }

    [return: NotNullIfNotNull("positionHistory")]
    public static PositionHistoryMongoDb? ConvertMongo(BasePositionHistory? positionHistory)
    {
        if (positionHistory == null)
            return null;

        return new PositionHistoryMongoDb(
            positionHistory.PositionId,
            positionHistory.EmployeeId,
            positionHistory.StartDate.ToDateTime(TimeOnly.MinValue),
            positionHistory.EndDate?.ToDateTime(TimeOnly.MinValue));
    }

    [return: NotNullIfNotNull("positionHistoryDb")]
    public static BasePositionHistory? ConvertMongo(PositionHistoryMongoDb? positionHistoryDb)
    {
        if (positionHistoryDb == null)
            return null;

        return new BasePositionHistory(
            positionHistoryDb.PositionId,
            positionHistoryDb.EmployeeId,
            DateOnly.FromDateTime(positionHistoryDb.StartDate),
            positionHistoryDb.EndDate.HasValue ? DateOnly.FromDateTime(positionHistoryDb.EndDate.Value) : null);
    }
}