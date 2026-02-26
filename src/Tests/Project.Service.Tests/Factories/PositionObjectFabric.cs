using Project.Core.Models.Position;

namespace Project.Service.Tests.Factories;

/// <summary>
/// Object Fabric для создания тестовых объектов Position.
/// </summary>
public static class PositionObjectFabric
{
    private static int _counter;

    /// <summary>
    /// Создаёт валидный объект CreatePosition.
    /// </summary>
    public static CreatePosition CreateValidCreatePosition(Guid companyId, Guid? parentId = null, string? title = null)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new CreatePosition(
            parentId,
            title ?? $"Должность {idx}",
            companyId
        );
    }
}

