using Project.Core.Models.Score;

namespace Project.Core.Services;

public interface IScoreService
{
    Task<BaseScore> AddScoreAsync(Guid employeeId, Guid authorId, Guid positionId, DateTimeOffset createdAt,
        int efficiencyScore, int engagementScore, int competencyScore);
    Task<BaseScore> UpdateScoreAsync(Guid id, DateTimeOffset? createdAt, int? efficiencyScore, int? engagementScore,
        int? competencyScore);
    Task<BaseScore> GetScoreAsync(Guid id);
    Task<IEnumerable<BaseScore>> GetScoresAsync(DateTimeOffset? startDate, DateTimeOffset? endDate);

    Task<IEnumerable<BaseScore>> GetScoresByEmployeeIdAsync(Guid employeeId, DateTimeOffset? startDate,
        DateTimeOffset? endDate);

    Task<IEnumerable<BaseScore>> GetScoresByAuthorIdAsync(Guid authorId, DateTimeOffset? startDate,
        DateTimeOffset? endDate);

    Task<IEnumerable<BaseScore>> GetScoresSubordinatesByEmployeeAsync(Guid employeeId,
        DateTimeOffset? startDate, DateTimeOffset? endDate);

    Task<IEnumerable<BaseScore>> GetScoresByPositionIdAsync(Guid positionId, DateTimeOffset? startDate,
        DateTimeOffset? endDate);

    Task DeleteScoreAsync(Guid id);
}