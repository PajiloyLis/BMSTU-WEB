using Database.Models;
using Project.Core.Models.Position;

namespace Project.Repository.Tests.Factories;

/// <summary>
/// Object Fabric для создания тестовых объектов Position (DB и Core модели).
/// </summary>
public static class PositionDbObjectFabric
{
    private static int _counter;

    /// <summary>
    /// Создаёт валидный PositionDb для прямой вставки в БД.
    /// </summary>
    public static PositionDb CreateValidPositionDb(Guid companyId, Guid? parentId = null, string? title = null)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new PositionDb(
            Guid.NewGuid(),
            parentId,
            title ?? $"Position {idx}",
            companyId
        );
    }

    /// <summary>
    /// Создаёт валидный CreatePosition для вызова репозитория.
    /// </summary>
    public static CreatePosition CreateValidCreatePosition(Guid companyId, Guid? parentId = null, string? title = null)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new CreatePosition(
            parentId,
            title ?? $"Position {idx}",
            companyId
        );
    }
}

