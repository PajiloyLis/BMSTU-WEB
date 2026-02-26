using Project.Core.Models.Post;

namespace Project.Service.Tests.Factories;

/// <summary>
/// Object Fabric для создания тестовых объектов Post.
/// </summary>
public static class PostObjectFabric
{
    private static int _counter;

    /// <summary>
    /// Создаёт валидный объект CreatePost.
    /// </summary>
    public static CreatePost CreateValidCreatePost(Guid companyId, string? title = null, decimal salary = 100000)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new CreatePost(
            title ?? $"Пост {idx}",
            salary,
            companyId
        );
    }

    /// <summary>
    /// Создаёт валидный объект UpdatePost.
    /// </summary>
    public static UpdatePost CreateValidUpdatePost(Guid postId, string? title = null, decimal? salary = null)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new UpdatePost(
            postId,
            title ?? $"Обновлённый пост {idx}",
            salary ?? 150000
        );
    }
}

