using System.Threading;
using Project.Dto.Http.Post;

namespace Project.Integration.Tests.Factories;

public static class PostObjectFabric
{
    private static int _counter;

    public static CreatePostDto CreatePostDto(Guid companyId, string? title = null, decimal salary = 100000m)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new CreatePostDto(
            title ?? $"Integration Post {idx}",
            salary,
            companyId);
    }

    public static UpdatePostDto UpdatePostDto(string title = "Updated Integration Post", decimal salary = 120000m)
    {
        return new UpdatePostDto(title, salary);
    }
}

