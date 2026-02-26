using Project.Core.Models.Score;

namespace Project.Service.Tests.Factories;

/// <summary>
/// Object Fabric для создания тестовых объектов Score.
/// </summary>
public static class ScoreObjectFabric
{
    /// <summary>
    /// Создаёт валидный объект CreateScore.
    /// </summary>
    public static CreateScore CreateValidCreateScore(
        Guid employeeId, Guid authorId, Guid positionId,
        int efficiency = 4, int engagement = 5, int competency = 3)
    {
        return new CreateScore(
            employeeId,
            authorId,
            positionId,
            DateTimeOffset.UtcNow.AddDays(-1),
            efficiency,
            engagement,
            competency
        );
    }

    /// <summary>
    /// Создаёт валидный объект UpdateScore.
    /// </summary>
    public static UpdateScore CreateValidUpdateScore(
        Guid scoreId, int? efficiency = 5, int? engagement = 4, int? competency = 3)
    {
        return new UpdateScore(
            scoreId,
            DateTimeOffset.UtcNow.AddDays(-1),
            efficiency,
            engagement,
            competency
        );
    }
}

