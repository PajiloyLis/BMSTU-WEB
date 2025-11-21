namespace Project.Dto.Http.Score;

public class UpdateScoreDto
{
    public UpdateScoreDto(DateTimeOffset? createdAt = null, int? efficiencyScore = null,
        int? engagementScore = null, int? competencyScore = null)
    {
        CreatedAt = createdAt;
        EfficiencyScore = efficiencyScore;
        EngagementScore = engagementScore;
        CompetencyScore = competencyScore;
    }

    public DateTimeOffset? CreatedAt { get; set; }
    public int? EfficiencyScore { get; set; }
    public int? EngagementScore { get; set; }
    public int? CompetencyScore { get; set; }
}