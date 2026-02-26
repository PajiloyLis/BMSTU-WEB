using Database.Models;
using Project.Core.Models.Post;

namespace Project.Repository.Tests.Factories;

/// <summary>
/// Object Fabric для создания тестовых объектов Post (DB и Core модели).
/// </summary>
public static class PostDbObjectFabric
{
    private static int _counter;

    /// <summary>
    /// Создаёт валидный PostDb для прямой вставки в БД.
    /// </summary>
    public static PostDb CreateValidPostDb(Guid companyId, string? title = null)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new PostDb(
            Guid.NewGuid(),
            title ?? $"Post {idx}",
            50000 + idx * 1000,
            companyId
        );
    }

    /// <summary>
    /// Создаёт валидный CreatePost для вызова репозитория.
    /// </summary>
    public static CreatePost CreateValidCreatePost(Guid companyId, string? title = null)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new CreatePost(
            title ?? $"Post {idx}",
            50000 + idx * 1000,
            companyId
        );
    }

    /// <summary>
    /// Создаёт валидный UpdatePost.
    /// </summary>
    public static UpdatePost CreateValidUpdatePost(Guid postId, string? title = null, decimal? salary = null)
    {
        var idx = Interlocked.Increment(ref _counter);
        return new UpdatePost(
            postId,
            title ?? $"Updated Post {idx}",
            salary ?? 70000 + idx * 1000
        );
    }
}

