// using Database.Context;
// using Database.Models;
// using Database.Models.Converters;
// using Microsoft.Extensions.Logging;
// using MongoDB.Driver;
// using Project.Core.Exceptions;
// using Project.Core.Models.PositionHistory;
// using Project.Core.Models.Score;
// using Project.Core.Repositories;
// using Project.Database.Models;
//
// namespace Database.Repositories;
//
// public class ScoreRepositoryMongo : IScoreRepository
// {
//     private readonly IMongoCollection<ScoreMongoDb> _scores;
//     private readonly IMongoCollection<PositionHistoryMongoDb> _positionHistories;
//     private readonly IMongoCollection<PositionMongoDb> _positions;
//     private readonly ILogger<ScoreRepository> _logger;
//
//     public ScoreRepositoryMongo(MongoDbContext context, ILogger<ScoreRepository> logger)
//     {
//         _scores = context.Scores;
//         _positionHistories = context.PositionHistories;
//         _positions = context.Positions;
//         _logger = logger;
//         
//         // Создаем индексы для производительности
//         // CreateIndexes();
//     }
//
//     private void CreateIndexes()
//     {
//         // Индекс для поиска по EmployeeId
//         _scores.Indexes.CreateOne(
//             new CreateIndexModel<ScoreMongoDb>(
//                 Builders<ScoreMongoDb>.IndexKeys.Ascending(s => s.EmployeeId)
//             ));
//
//         // Индекс для поиска по AuthorId
//         _scores.Indexes.CreateOne(
//             new CreateIndexModel<ScoreMongoDb>(
//                 Builders<ScoreMongoDb>.IndexKeys.Ascending(s => s.AuthorId)
//             ));
//
//         // Индекс для поиска по PositionId
//         _scores.Indexes.CreateOne(
//             new CreateIndexModel<ScoreMongoDb>(
//                 Builders<ScoreMongoDb>.IndexKeys.Ascending(s => s.PositionId)
//             ));
//
//         // Индекс для сортировки по дате создания
//         _scores.Indexes.CreateOne(
//             new CreateIndexModel<ScoreMongoDb>(
//                 Builders<ScoreMongoDb>.IndexKeys.Ascending(s => s.CreatedAt)
//             ));
//     }
//
//     public async Task<BaseScore> AddScoreAsync(CreateScore score)
//     {
//         var scoreDb = ScoreConverter.ConvertMongo(score);
//         scoreDb.UpdatedAt = DateTime.UtcNow;
//
//         try
//         {
//             await _scores.InsertOneAsync(scoreDb);
//             
//             _logger.LogInformation("Score with id {Id} was added", scoreDb.Id);
//             return ScoreConverter.ConvertMongo(scoreDb);
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, "Error adding score for employee {EmployeeId}", score.EmployeeId);
//             throw;
//         }
//     }
//
//     public async Task<BaseScore> GetScoreByIdAsync(Guid id)
//     {
//         try
//         {
//             var filter = Builders<ScoreMongoDb>.Filter.Eq(s => s.Id, id);
//             var score = await _scores.Find(filter).FirstOrDefaultAsync();
//             
//             if (score == null)
//             {
//                 _logger.LogWarning("Score with id {Id} not found", id);
//                 throw new ScoreNotFoundException($"Score with id {id} not found");
//             }
//
//             _logger.LogInformation("Score with id {Id} was found", id);
//             return ScoreConverter.ConvertMongo(score);
//         }
//         catch (ScoreNotFoundException e)
//         {
//             _logger.LogWarning(e, $"Score with id {id} not found");
//             throw;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error getting score with id - {id}");
//             throw;
//         }
//     }
//
//     public async Task<BaseScore> UpdateScoreAsync(UpdateScore score)
//     {
//         try
//         {
//             var filter = Builders<ScoreMongoDb>.Filter.Eq(s => s.Id, score.Id);
//             var scoreToUpdate = await _scores.Find(filter).FirstOrDefaultAsync();
//             
//             if (scoreToUpdate == null)
//                 throw new ScoreNotFoundException($"Score with id {score.Id} not found");
//
//             // Подготовка обновлений
//             var updateDefinition = Builders<ScoreMongoDb>.Update
//                 .Set(s => s.UpdatedAt, DateTime.UtcNow);
//
//             if (score.EfficiencyScore.HasValue)
//                 updateDefinition = updateDefinition.Set(s => s.EfficiencyScore, score.EfficiencyScore.Value);
//             if (score.EngagementScore.HasValue)
//                 updateDefinition = updateDefinition.Set(s => s.EngagementScore, score.EngagementScore.Value);
//             if (score.CompetencyScore.HasValue)
//                 updateDefinition = updateDefinition.Set(s => s.CompetencyScore, score.CompetencyScore.Value);
//
//             var options = new FindOneAndUpdateOptions<ScoreMongoDb> 
//             { 
//                 ReturnDocument = ReturnDocument.After 
//             };
//
//             var updatedScore = await _scores.FindOneAndUpdateAsync(
//                 filter, updateDefinition, options);
//
//             if (updatedScore == null)
//                 throw new ScoreNotFoundException($"Score with id {score.Id} not found after update");
//
//             _logger.LogInformation("Score with id {Id} was updated", score.Id);
//             return ScoreConverter.ConvertMongo(updatedScore);
//         }
//         catch (ScoreNotFoundException e)
//         {
//             _logger.LogWarning(e, $"Score with id {score.Id} not found");
//             throw;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error updating score with id - {score.Id}");
//             throw;
//         }
//     }
//
//     public async Task DeleteScoreAsync(Guid id)
//     {
//         try
//         {
//             var filter = Builders<ScoreMongoDb>.Filter.Eq(s => s.Id, id);
//             var result = await _scores.DeleteOneAsync(filter);
//
//             if (result.DeletedCount == 0)
//             {
//                 _logger.LogWarning("Score with id {Id} not found for deletion", id);
//                 throw new ScoreNotFoundException($"Score with id {id} not found");
//             }
//
//             _logger.LogInformation("Score with id {Id} was deleted", id);
//         }
//         catch (ScoreNotFoundException e)
//         {
//             _logger.LogWarning(e, $"Score with id {id} not found for deletion");
//             throw;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error deleting score with id - {id}");
//             throw;
//         }
//     }
//
//     public async Task<IEnumerable<BaseScore>> GetScoresAsync(DateTimeOffset? startDate,
//         DateTimeOffset? endDate)
//     {
//         try
//         {
//             var filterBuilder = Builders<ScoreMongoDb>.Filter;
//             var filter = filterBuilder.Empty;
//
//             if (startDate.HasValue)
//                 filter = filter & filterBuilder.Gte(s => s.CreatedAt, startDate.Value);
//             if (endDate.HasValue)
//                 filter = filter & filterBuilder.Lte(s => s.CreatedAt, endDate.Value);
//
//             var sort = Builders<ScoreMongoDb>.Sort.Descending(s => s.CreatedAt);
//
//             var scores = await _scores
//                 .Find(filter)
//                 .Sort(sort)
//                 .ToListAsync();
//
//             var baseScores = scores.Select(ScoreConverter.ConvertMongo).ToList();
//
//             _logger.LogInformation("Got {Count} scores", baseScores.Count);
//             return baseScores;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, "Error getting scores");
//             throw;
//         }
//     }
//
//     public async Task<IEnumerable<BaseScore>> GetScoresByEmployeeIdAsync(Guid employeeId, DateTimeOffset? startDate,
//         DateTimeOffset? endDate)
//     {
//         try
//         {
//             var filterBuilder = Builders<ScoreMongoDb>.Filter;
//             var filter = filterBuilder.Eq(s => s.EmployeeId, employeeId);
//
//             if (startDate.HasValue)
//                 filter = filter & filterBuilder.Gte(s => s.CreatedAt, startDate.Value);
//             if (endDate.HasValue)
//                 filter = filter & filterBuilder.Lte(s => s.CreatedAt, endDate.Value);
//
//             var sort = Builders<ScoreMongoDb>.Sort.Descending(s => s.CreatedAt);
//
//             var scores = await _scores
//                 .Find(filter)
//                 .Sort(sort)
//                 .ToListAsync();
//
//             var baseScores = scores.Select(ScoreConverter.ConvertMongo).ToList();
//
//             _logger.LogInformation("Got {Count} scores for employee {EmployeeId}", baseScores.Count, employeeId);
//             return baseScores;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error getting scores for employee - {employeeId}");
//             throw;
//         }
//     }
//
//     public async Task<IEnumerable<BaseScore>> GetScoresByPositionIdAsync(Guid positionId, DateTimeOffset? startDate,
//         DateTimeOffset? endDate)
//     {
//         try
//         {
//             var filterBuilder = Builders<ScoreMongoDb>.Filter;
//             var filter = filterBuilder.Eq(s => s.PositionId, positionId);
//
//             if (startDate.HasValue)
//                 filter = filter & filterBuilder.Gte(s => s.CreatedAt, startDate.Value);
//             if (endDate.HasValue)
//                 filter = filter & filterBuilder.Lte(s => s.CreatedAt, endDate.Value);
//
//             var sort = Builders<ScoreMongoDb>.Sort.Descending(s => s.CreatedAt);
//
//             var scores = await _scores
//                 .Find(filter)
//                 .Sort(sort)
//                 .ToListAsync();
//
//             var baseScores = scores.Select(ScoreConverter.ConvertMongo).ToList();
//
//             _logger.LogInformation("Got {Count} scores for position {PositionId}", baseScores.Count, positionId);
//             return baseScores;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error getting scores for position - {positionId}");
//             throw;
//         }
//     }
//
//     public async Task<IEnumerable<BaseScore>> GetScoresByAuthorIdAsync(Guid authorId, DateTimeOffset? startDate,
//         DateTimeOffset? endDate)
//     {
//         try
//         {
//             var filterBuilder = Builders<ScoreMongoDb>.Filter;
//             var filter = filterBuilder.Eq(s => s.AuthorId, authorId);
//
//             if (startDate.HasValue)
//                 filter = filter & filterBuilder.Gte(s => s.CreatedAt, startDate.Value);
//             if (endDate.HasValue)
//                 filter = filter & filterBuilder.Lte(s => s.CreatedAt, endDate.Value);
//
//             var sort = Builders<ScoreMongoDb>.Sort.Descending(s => s.CreatedAt);
//
//             var scores = await _scores
//                 .Find(filter)
//                 .Sort(sort)
//                 .ToListAsync();
//
//             var baseScores = scores.Select(ScoreConverter.ConvertMongo).ToList();
//
//             _logger.LogInformation("Got {Count} scores by author {AuthorId}", baseScores.Count, authorId);
//             return baseScores;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error getting scores by author - {authorId}");
//             throw;
//         }
//     }
//
//     public async Task<IEnumerable<BaseScore>> GetScoresSubordinatesByEmployeeIdAsync(Guid employeeId,
//         DateTimeOffset? startDate, DateTimeOffset? endDate)
//     {
//         try
//         {
//             _logger.LogInformation("Getting subordinate scores for employee {EmployeeId}", employeeId);
//             var subordinates = await GetAllCurrentSubordinates(employeeId);
//             var employees = subordinates.Select(e => e.EmployeeId).ToList();
//
//             if (employees == null)
//             {
//                 _logger.LogWarning("Employee {EmployeeId} has no position assigned", employeeId);
//                 throw new ScoreNotFoundException($"Employee {employeeId} has no position assigned");
//             }
//
//             var filter = Builders<ScoreMongoDb>.Filter.In(s => s.EmployeeId, employees);
//             if (startDate.HasValue)
//                 filter = filter & Builders<ScoreMongoDb>.Filter.Gte(s => s.CreatedAt, startDate.Value);
//             if (endDate.HasValue)
//                 filter = filter & Builders<ScoreMongoDb>.Filter.Lte(s => s.CreatedAt, endDate.Value);
//             var scores = await _scores.Find(filter).ToListAsync();
//             var baseScores = scores.Select(ScoreConverter.ConvertMongo).ToList();
//             _logger.LogInformation("Got {Count} scores for subordinates of employee {EmployeeId}", baseScores.Count, employeeId);
//             return baseScores;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error getting subordinate scores for employee - {employeeId}");
//             throw;
//         }
//     }
//
//     private async Task<List<PositionHierarchyWithEmployee>> GetAllCurrentSubordinates(Guid managerId)
//     {
//         try
//         {
//             var subordinates = new List<PositionHierarchyWithEmployee>();
//             var head = await _positionHistories.Find(ph => ph.EmployeeId == managerId && ph.EndDate == null).FirstOrDefaultAsync();
//             if (head is null)
//                 throw new PositionHistoryNotFoundException($"Current position not found for employee {managerId}");
//             var headPosition = await _positions.Find(p => p.Id == head.PositionId).FirstOrDefaultAsync();
//             subordinates.Add(new PositionHierarchyWithEmployee(head.EmployeeId, head.PositionId, headPosition.ParentId, headPosition.Title, 0));
//             int i = 0;
//             while (i != subordinates.Count)
//             {
//                 var subordinatesPositions = await _positions.Find(p => p.ParentId == subordinates[i].PositionId).ToListAsync();
//                 var children = await _positionHistories.Find(ph => subordinatesPositions.Select(p => p.Id).ToList().Contains(ph.PositionId) && ph.EndDate == null).ToListAsync();
//                 subordinates.AddRange(children.Select(e => new PositionHierarchyWithEmployee(e.EmployeeId, e.PositionId, subordinatesPositions.Where(p => p.Id == e.PositionId).FirstOrDefault().ParentId, subordinatesPositions.Where(p => p.Id == e.PositionId).FirstOrDefault().Title, subordinates[i].Level + 1)));
//                 ++i;
//             }
//             return subordinates;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error getting current subordinates for manager - {managerId}");
//             throw;
//         }
//     }
// }
