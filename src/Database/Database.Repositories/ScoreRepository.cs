using Database.Context;
using Database.Models.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Project.Core.Exceptions;
using Project.Core.Models;
using Project.Core.Models.PositionHistory;
using Project.Core.Models.Score;
using Project.Core.Repositories;


namespace Database.Repositories;

public class ScoreRepository : IScoreRepository
{
    private readonly ProjectDbContext _context;
    private readonly ILogger<ScoreRepository> _logger;

    public ScoreRepository(ProjectDbContext context, ILogger<ScoreRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BaseScore> AddScoreAsync(CreateScore score)
    {
        try
        {
            var scoreDb = ScoreConverter.Convert(score);
            if (scoreDb is null)
            {
                _logger.LogWarning("Failed to convert CreateScore to ScoreDb");
                throw new ArgumentException("Failed to convert CreateScore to ScoreDb");
            }

            await _context.ScoreDb.AddAsync(scoreDb);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Score with id {Id} was added", scoreDb.Id);
            return ScoreConverter.Convert(scoreDb)!;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while adding score");
            throw;
        }
    }

    public async Task<BaseScore> GetScoreByIdAsync(Guid id)
    {
        try
        {
            var score = await _context.ScoreDb
                .FirstOrDefaultAsync(s => s.Id == id);

            if (score is null)
            {
                _logger.LogWarning("Score with id {Id} not found", id);
                throw new ScoreNotFoundException($"Score with id {id} not found");
            }

            _logger.LogInformation("Score with id {Id} was retrieved", id);
            return ScoreConverter.Convert(score)!;
        }
        catch (Exception e) when (e is not ScoreNotFoundException)
        {
            _logger.LogError(e, "Error occurred while getting score with id {Id}", id);
            throw;
        }
    }

    public async Task<BaseScore> UpdateScoreAsync(UpdateScore score)
    {
        try
        {
            var scoreDb = await _context.ScoreDb
                .FirstOrDefaultAsync(s => s.Id == score.Id);

            if (scoreDb is null)
            {
                _logger.LogWarning("Score with id {Id} not found for update", score.Id);
                throw new ScoreNotFoundException($"Score with id {score.Id} not found");
            }

            scoreDb.EfficiencyScore = score.EfficiencyScore ?? scoreDb.EfficiencyScore;
            scoreDb.EngagementScore = score.EngagementScore ?? scoreDb.EngagementScore;
            scoreDb.CompetencyScore = score.CompetencyScore ?? scoreDb.CompetencyScore;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Score with id {Id} was updated", score.Id);
            return ScoreConverter.Convert(scoreDb)!;
        }
        catch (Exception e) when (e is not ScoreNotFoundException)
        {
            _logger.LogError(e, "Error occurred while updating score with id {Id}", score.Id);
            throw;
        }
    }

    public async Task DeleteScoreAsync(Guid id)
    {
        try
        {
            var score = await _context.ScoreDb
                .FirstOrDefaultAsync(s => s.Id == id);

            if (score is null)
            {
                _logger.LogWarning("Score with id {Id} not found for deletion", id);
                throw new ScoreNotFoundException($"Score with id {id} not found");
            }

            _context.ScoreDb.Remove(score);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Score with id {Id} was deleted", id);
        }
        catch (Exception e) when (e is not ScoreNotFoundException)
        {
            _logger.LogError(e, "Error occurred while deleting score with id {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<BaseScore>> GetScoresAsync(DateTimeOffset? startDate,
        DateTimeOffset? endDate)
    {
        try
        {
            var query = _context.ScoreDb.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(s => s.CreatedAt >= startDate.Value.ToUniversalTime());
            if (endDate.HasValue)
                query = query.Where(s => s.CreatedAt <= endDate.Value.ToUniversalTime());

            var scores = await query
                .Select(s => ScoreConverter.Convert(s)!)
                .ToListAsync();

            _logger.LogInformation("Scores were retrieved");
            return scores;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while getting scores");
            throw;
        }
    }

    public async Task<IEnumerable<BaseScore>> GetScoresByEmployeeIdAsync(Guid employeeId,
        DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        try
        {
            var query = _context.ScoreDb.Where(s => s.EmployeeId == employeeId);

            if (startDate.HasValue)
                query = query.Where(s => s.CreatedAt >= startDate.Value.ToUniversalTime());
            if (endDate.HasValue)
                query = query.Where(s => s.CreatedAt <= endDate.Value.ToUniversalTime());

            var scores = await query
                .Select(s => ScoreConverter.Convert(s)!)
                .ToListAsync();


            _logger.LogInformation("Scores for employee {EmployeeId} were retrieved",
                employeeId);
            return scores;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while getting scores for employee {EmployeeId}", employeeId);
            throw;
        }
    }

    public async Task<IEnumerable<BaseScore>> GetScoresByPositionIdAsync(Guid positionId,
        DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        try
        {
            var query = _context.ScoreDb.Where(s => s.PositionId == positionId);

            if (startDate.HasValue)
                query = query.Where(s => s.CreatedAt >= startDate.Value.DateTime);
            if (endDate.HasValue)
                query = query.Where(s => s.CreatedAt <= endDate.Value.DateTime);

            var scores = await query
                .Select(s => ScoreConverter.Convert(s)!)
                .ToListAsync();

            _logger.LogInformation("Scores for position {PositionId} were retrieved",
                positionId);
            return scores;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while getting scores for position {PositionId}", positionId);
            throw;
        }
    }

    public async Task<IEnumerable<BaseScore>> GetScoresByAuthorIdAsync(Guid authorId,
        DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        try
        {
            var query = _context.ScoreDb.Where(s => s.AuthorId == authorId);

            if (startDate.HasValue)
                query = query.Where(s => s.CreatedAt >= startDate.Value.DateTime);
            if (endDate.HasValue)
                query = query.Where(s => s.CreatedAt <= endDate.Value.DateTime);

            var scores = await query
                .Select(s => ScoreConverter.Convert(s)!)
                .ToListAsync();

            _logger.LogInformation("Scores by author {AuthorId} were retrieved",
                authorId);
            return scores;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while getting scores by author {AuthorId}", authorId);
            throw;
        }
    }

    public async Task<IEnumerable<BaseScore>> GetScoresSubordinatesByEmployeeIdAsync(Guid employeeId,
        DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        try
        {
            var subordinates = await GetAllCurrentSubordinates(employeeId);
            var employees = subordinates.Select(x => x.EmployeeId).ToList();
            
            if (employees == null)
            {
                _logger.LogWarning("Employee {EmployeeId} has no position assigned", employeeId);
                throw new ScoreNotFoundException($"Employee {employeeId} has no position assigned");
            }

            var query = _context.ScoreDb.Where(s => employees.Contains(s.EmployeeId));

            if (startDate.HasValue)
                query = query.Where(s => s.CreatedAt >= startDate.Value.ToUniversalTime());
            if (endDate.HasValue)
                query = query.Where(s => s.CreatedAt <= endDate.Value.ToUniversalTime());

            var scores = await query
                .Select(s => ScoreConverter.Convert(s)!)
                .ToListAsync();

            _logger.LogInformation(
                "Scores for subordinates of employee {EmployeeId} were retrieved",
                employeeId);
            return scores;
        }
        catch (Exception e) when (e is not ScoreNotFoundException)
        {
            _logger.LogError(e, "Error occurred while getting scores for subordinates of employee {EmployeeId}",
                employeeId);
            throw;
        }
    }
    
    private async Task<List<PositionHierarchyWithEmployee>> GetAllCurrentSubordinates(Guid managerId)
    {
        try
        {
            List<PositionHierarchyWithEmployee> subordinates = new List<PositionHierarchyWithEmployee>();
            var head = await _context.PositionHistoryDb.Where(e => e.EmployeeId == managerId && e.EndDate==null).FirstOrDefaultAsync();
            if (head is null)
                throw new PositionHistoryNotFoundException($"Current position for employee {managerId} not found");
            var headPosition = await _context.PositionDb.Where(e => e.Id == head.PositionId).FirstOrDefaultAsync();
            subordinates.Add(new PositionHierarchyWithEmployee(head.EmployeeId, head.PositionId, headPosition.ParentId, headPosition.Title, 0));
            int i = 0;
            while (i != subordinates.Count)
            {
                var subordinatesPositions =
                    await _context.PositionDb.Where(e => e.ParentId == subordinates[i].PositionId).ToListAsync();
                var children = await _context.PositionHistoryDb.Where(e => subordinatesPositions.Select(e => e.Id).ToList().Contains(e.PositionId) && e.EndDate == null)
                    .ToListAsync();
                var resultChildren = children.Select(e =>
                {
                    var position = subordinatesPositions.Where(p => p.Id == e.PositionId).FirstOrDefault();
                    return new PositionHierarchyWithEmployee(e.EmployeeId, e.PositionId, position.ParentId,
                        position.Title,
                        subordinates[i].Level + 1);
                });
                ++i;
            }

            return subordinates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting current subordinates for manager {ManagerId}",
                managerId);
            throw;
        }
    }
}