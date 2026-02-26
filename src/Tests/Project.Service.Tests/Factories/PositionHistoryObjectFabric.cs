using Project.Core.Models.PositionHistory;

namespace Project.Service.Tests.Factories;

/// <summary>
/// Object Fabric для создания тестовых объектов PositionHistory.
/// </summary>
public static class PositionHistoryObjectFabric
{
    /// <summary>
    /// Создаёт валидный объект CreatePositionHistory.
    /// </summary>
    public static CreatePositionHistory CreateValidCreatePositionHistory(
        Guid positionId, Guid employeeId,
        DateOnly? startDate = null, DateOnly? endDate = null)
    {
        return new CreatePositionHistory(
            positionId,
            employeeId,
            startDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            endDate
        );
    }

    /// <summary>
    /// Создаёт CreatePositionHistory с датой начала и окончания.
    /// </summary>
    public static CreatePositionHistory CreateCompletedPositionHistory(
        Guid positionId, Guid employeeId)
    {
        return new CreatePositionHistory(
            positionId,
            employeeId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-60)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10))
        );
    }
}

