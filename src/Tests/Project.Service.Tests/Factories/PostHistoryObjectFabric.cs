using Project.Core.Models.PostHistory;

namespace Project.Service.Tests.Factories;

/// <summary>
/// Object Fabric для создания тестовых объектов PostHistory.
/// </summary>
public static class PostHistoryObjectFabric
{
    /// <summary>
    /// Создаёт валидный объект CreatePostHistory.
    /// </summary>
    public static CreatePostHistory CreateValidCreatePostHistory(
        Guid postId, Guid employeeId,
        DateOnly? startDate = null, DateOnly? endDate = null)
    {
        return new CreatePostHistory(
            postId,
            employeeId,
            startDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            endDate
        );
    }

    /// <summary>
    /// Создаёт завершённый PostHistory (с endDate).
    /// </summary>
    public static CreatePostHistory CreateCompletedPostHistory(
        Guid postId, Guid employeeId)
    {
        return new CreatePostHistory(
            postId,
            employeeId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-60)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10))
        );
    }
}

