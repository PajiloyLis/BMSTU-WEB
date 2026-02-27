using Project.Dto.Http.Score;

namespace Project.Integration.Tests.Factories;

public static class ScoreObjectFabric
{
    public static CreateScoreDto CreateScoreDto(
        Guid employeeId,
        Guid authorId,
        Guid positionId,
        DateTimeOffset? createdAt = null,
        int efficiencyScore = 4,
        int engagementScore = 4,
        int competencyScore = 4)
    {
        return new CreateScoreDto(
            employeeId,
            authorId,
            positionId,
            createdAt ?? DateTimeOffset.UtcNow.AddDays(-1),
            efficiencyScore,
            engagementScore,
            competencyScore);
    }

    public static UpdateScoreDto UpdateScoreDto(
        DateTimeOffset? createdAt = null,
        int? efficiencyScore = 5,
        int? engagementScore = 5,
        int? competencyScore = 5)
    {
        return new UpdateScoreDto(
            createdAt ?? DateTimeOffset.UtcNow.AddDays(-2),
            efficiencyScore,
            engagementScore,
            competencyScore);
    }
}

