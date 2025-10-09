using Project.Core.Models.Score;

namespace Project.Core.Repositories;

public interface IScoreRepository
{
    Task<BaseScore> AddScoreAsync(CreateScore score);
    Task<BaseScore> GetScoreByIdAsync(Guid id);
    Task<BaseScore> UpdateScoreAsync(UpdateScore score);
    Task DeleteScoreAsync(Guid id);
    Task<IEnumerable<BaseScore>> GetScoresAsync(DateTimeOffset? startDate,
        DateTimeOffset? endDate);

    Task<IEnumerable<BaseScore>> GetScoresByEmployeeIdAsync(Guid employeeId, DateTimeOffset? startDate,
        DateTimeOffset? endDate);

    Task<IEnumerable<BaseScore>> GetScoresByPositionIdAsync(Guid positionId, DateTimeOffset? startDate,
        DateTimeOffset? endDate);

    Task<IEnumerable<BaseScore>> GetScoresByAuthorIdAsync(Guid authorId, DateTimeOffset? startDate,
        DateTimeOffset? endDate);

    Task<IEnumerable<BaseScore>> GetScoresSubordinatesByEmployeeIdAsync(Guid employeeId,
        DateTimeOffset? startDate, DateTimeOffset? endDate);
}