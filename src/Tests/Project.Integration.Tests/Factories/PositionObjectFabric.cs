using System.Threading;
using Project.Dto.Http.Position;

namespace Project.Integration.Tests.Factories;

public static class PositionObjectFabric
{
    private static int _counter;

    public static CreatePositionDto CreatePositionDto(Guid companyId, Guid? parentId = null, string? title = null)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new CreatePositionDto(
            parentId,
            title ?? $"Integration Position {idx}",
            companyId);
    }
}

