// using Database.Context;
// using Database.Models;
// using Database.Models.Converters;
// using Microsoft.Extensions.Logging;
// using MongoDB.Driver;
// using Project.Core.Exceptions;
// using Project.Core.Models.PostHistory;
// using Project.Core.Repositories;
// using Project.Database.Models;
//
// namespace Database.Repositories;
//
// public class PostHistoryRepositoryMongo : IPostHistoryRepository
// {
//     private readonly IMongoCollection<PostHistoryMongoDb> _postHistories;
//     private readonly IMongoCollection<PositionMongoDb> _positions;
//     private readonly IMongoCollection<PositionHistoryMongoDb> _positionHistories;
//     private readonly ILogger<PostHistoryRepository> _logger;
//
//     public PostHistoryRepositoryMongo(MongoDbContext context, ILogger<PostHistoryRepository> logger)
//     {
//         _postHistories = context.PostHistories;
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
//         // Составной индекс для уникальности
//         _postHistories.Indexes.CreateOne(
//             new CreateIndexModel<PostHistoryMongoDb>(
//                 Builders<PostHistoryMongoDb>.IndexKeys
//                     .Ascending(ph => ph.PostId)
//                     .Ascending(ph => ph.EmployeeId)
//                     .Ascending(ph => ph.StartDate),
//                 new CreateIndexOptions { Unique = true }
//             ));
//
//         // Индекс для поиска по EmployeeId
//         _postHistories.Indexes.CreateOne(
//             new CreateIndexModel<PostHistoryMongoDb>(
//                 Builders<PostHistoryMongoDb>.IndexKeys.Ascending(ph => ph.EmployeeId)
//             ));
//
//         // Индекс для поиска по PostId
//         _postHistories.Indexes.CreateOne(
//             new CreateIndexModel<PostHistoryMongoDb>(
//                 Builders<PostHistoryMongoDb>.IndexKeys.Ascending(ph => ph.PostId)
//             ));
//
//         // Индекс для сортировки по дате начала
//         _postHistories.Indexes.CreateOne(
//             new CreateIndexModel<PostHistoryMongoDb>(
//                 Builders<PostHistoryMongoDb>.IndexKeys.Ascending(ph => ph.StartDate)
//             ));
//     }
//
//     public async Task<BasePostHistory> AddPostHistoryAsync(CreatePostHistory createPostHistory)
//     {
//         var postHistoryDb = PostHistoryConverter.ConvertMongo(createPostHistory);
//         postHistoryDb.CreatedAt = DateTime.UtcNow;
//         postHistoryDb.UpdatedAt = DateTime.UtcNow;
//
//         try
//         {
//             // Проверка на существующую запись истории
//             var duplicateFilter = Builders<PostHistoryMongoDb>.Filter.And(
//                 Builders<PostHistoryMongoDb>.Filter.Eq(ph => ph.PostId, postHistoryDb.PostId),
//                 Builders<PostHistoryMongoDb>.Filter.Eq(ph => ph.EmployeeId, postHistoryDb.EmployeeId),
//                 Builders<PostHistoryMongoDb>.Filter.Eq(ph => ph.StartDate, postHistoryDb.StartDate)
//             );
//
//             var existingPostHistory = await _postHistories.Find(duplicateFilter).FirstOrDefaultAsync();
//             if (existingPostHistory != null)
//             {
//                 _logger.LogWarning("Post history already exists for post {PostId} and employee {EmployeeId}", 
//                     createPostHistory.PostId, createPostHistory.EmployeeId);
//                 throw new PostHistoryNotFoundException(
//                     $"Post history already exists for post {createPostHistory.PostId} and employee {createPostHistory.EmployeeId}");
//             }
//
//             await _postHistories.InsertOneAsync(postHistoryDb);
//
//             _logger.LogInformation("Post history for post {PostId} and employee {EmployeeId} was added", 
//                 postHistoryDb.PostId, postHistoryDb.EmployeeId);
//             return PostHistoryConverter.ConvertMongo(postHistoryDb);
//         }
//         catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
//         {
//             _logger.LogWarning(ex, "Post history already exists for post {PostId} and employee {EmployeeId}", 
//                 createPostHistory.PostId, createPostHistory.EmployeeId);
//             throw new PostHistoryNotFoundException(
//                 $"Post history already exists for post {createPostHistory.PostId} and employee {createPostHistory.EmployeeId}");
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, "Error adding post history for post {PostId} and employee {EmployeeId}", 
//                 createPostHistory.PostId, createPostHistory.EmployeeId);
//             throw;
//         }
//     }
//
//     public async Task<BasePostHistory> GetPostHistoryByIdAsync(Guid postId, Guid employeeId)
//     {
//         try
//         {
//             var filter = Builders<PostHistoryMongoDb>.Filter.And(
//                 Builders<PostHistoryMongoDb>.Filter.Eq(ph => ph.PostId, postId),
//                 Builders<PostHistoryMongoDb>.Filter.Eq(ph => ph.EmployeeId, employeeId)
//             );
//             
//             var postHistory = await _postHistories.Find(filter).FirstOrDefaultAsync();
//             
//             if (postHistory == null)
//             {
//                 _logger.LogWarning("Post history for post {PostId} and employee {EmployeeId} not found", postId, employeeId);
//                 throw new PostHistoryNotFoundException(
//                     $"Post history for post {postId} and employee {employeeId} not found");
//             }
//
//             _logger.LogInformation("Post history for post {PostId} and employee {EmployeeId} was found", postId, employeeId);
//             return PostHistoryConverter.ConvertMongo(postHistory);
//         }
//         catch (PostHistoryNotFoundException e)
//         {
//             _logger.LogWarning(e, $"Post history for post {postId} and employee {employeeId} not found");
//             throw;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error getting post history for post {postId} and employee {employeeId}");
//             throw;
//         }
//     }
//
//     public async Task<BasePostHistory> UpdatePostHistoryAsync(UpdatePostHistory updatePostHistory)
//     {
//         try
//         {
//             var filter = Builders<PostHistoryMongoDb>.Filter.And(
//                 Builders<PostHistoryMongoDb>.Filter.Eq(ph => ph.PostId, updatePostHistory.PostId),
//                 Builders<PostHistoryMongoDb>.Filter.Eq(ph => ph.EmployeeId, updatePostHistory.EmployeeId)
//             );
//
//             var postHistoryToUpdate = await _postHistories.Find(filter).FirstOrDefaultAsync();
//             if (postHistoryToUpdate == null)
//                 throw new PostHistoryNotFoundException(
//                     $"Post history for post {updatePostHistory.PostId} and employee {updatePostHistory.EmployeeId} not found");
//
//             // Подготовка обновлений
//             var updateDefinition = Builders<PostHistoryMongoDb>.Update
//                 .Set(ph => ph.UpdatedAt, DateTime.UtcNow);
//
//             if (updatePostHistory.StartDate.HasValue)
//                 updateDefinition = updateDefinition.Set(ph => ph.StartDate, updatePostHistory.StartDate.Value.ToDateTime(TimeOnly.MinValue));
//             if (updatePostHistory.EndDate.HasValue)
//                 updateDefinition = updateDefinition.Set(ph => ph.EndDate, updatePostHistory.EndDate.Value.ToDateTime(TimeOnly.MinValue));
//
//             var options = new FindOneAndUpdateOptions<PostHistoryMongoDb> 
//             { 
//                 ReturnDocument = ReturnDocument.After 
//             };
//
//             var updatedPostHistory = await _postHistories.FindOneAndUpdateAsync(
//                 filter, updateDefinition, options);
//
//             if (updatedPostHistory == null)
//                 throw new PostHistoryNotFoundException(
//                     $"Post history for post {updatePostHistory.PostId} and employee {updatePostHistory.EmployeeId} not found after update");
//
//             _logger.LogInformation("Post history for post {PostId} and employee {EmployeeId} was updated", 
//                 updatePostHistory.PostId, updatePostHistory.EmployeeId);
//             return PostHistoryConverter.ConvertMongo(updatedPostHistory);
//         }
//         catch (PostHistoryNotFoundException e)
//         {
//             _logger.LogWarning(e, $"Post history for post {updatePostHistory.PostId} and employee {updatePostHistory.EmployeeId} not found");
//             throw;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error updating post history for post {updatePostHistory.PostId} and employee {updatePostHistory.EmployeeId}");
//             throw;
//         }
//     }
//
//     public async Task DeletePostHistoryAsync(Guid postId, Guid employeeId)
//     {
//         try
//         {
//             var filter = Builders<PostHistoryMongoDb>.Filter.And(
//                 Builders<PostHistoryMongoDb>.Filter.Eq(ph => ph.PostId, postId),
//                 Builders<PostHistoryMongoDb>.Filter.Eq(ph => ph.EmployeeId, employeeId)
//             );
//
//             var result = await _postHistories.DeleteOneAsync(filter);
//
//             if (result.DeletedCount == 0)
//             {
//                 _logger.LogWarning("Post history for post {PostId} and employee {EmployeeId} not found for deletion", postId, employeeId);
//                 throw new PostHistoryNotFoundException(
//                     $"Post history for post {postId} and employee {employeeId} not found");
//             }
//
//             _logger.LogInformation("Post history for post {PostId} and employee {EmployeeId} was deleted", postId, employeeId);
//         }
//         catch (PostHistoryNotFoundException e)
//         {
//             _logger.LogWarning(e, $"Post history for post {postId} and employee {employeeId} not found for deletion");
//             throw;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error deleting post history for post {postId} and employee {employeeId}");
//             throw;
//         }
//     }
//
//     public async Task<IEnumerable<BasePostHistory>> GetPostHistoryByEmployeeIdAsync(Guid employeeId,
//         DateOnly? startDate,
//         DateOnly? endDate)
//     {
//         try
//         {
//             var filterBuilder = Builders<PostHistoryMongoDb>.Filter;
//             var filter = filterBuilder.Eq(ph => ph.EmployeeId, employeeId);
//
//             if (startDate.HasValue)
//                 filter = filter & filterBuilder.Gte(ph => ph.StartDate, startDate.Value.ToDateTime(TimeOnly.MinValue));
//             if (endDate.HasValue)
//                 filter = filter & filterBuilder.Lte(ph => ph.StartDate, endDate.Value.ToDateTime(TimeOnly.MinValue));
//
//             var sort = Builders<PostHistoryMongoDb>.Sort.Descending(ph => ph.StartDate);
//
//             var postHistories = await _postHistories
//                 .Find(filter)
//                 .Sort(sort)
//                 .ToListAsync();
//
//             var basePostHistories = postHistories.Select(PostHistoryConverter.ConvertMongo).ToList();
//
//             _logger.LogInformation("Got {Count} post histories for employee {EmployeeId}", basePostHistories.Count, employeeId);
//             return basePostHistories;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error getting post histories for employee - {employeeId}");
//             throw;
//         }
//     }
//
//     public async Task<IEnumerable<BasePostHistory>> GetSubordinatesPostHistoryAsync(Guid managerId,
//         DateOnly? startDate,
//         DateOnly? endDate)
//     {
//         try
//         {
//             _logger.LogInformation("Getting subordinate post histories for manager {ManagerId}", managerId);
//             var head = await _positionHistories.Find(ph => ph.EmployeeId == managerId && ph.EndDate == null).FirstOrDefaultAsync();
//             if (head is null)
//                 throw new PositionHistoryNotFoundException($"Current position not found for employee {managerId}");
//             var subordinatesPositionHistories = new List<PositionHistoryMongoDb>();
//             subordinatesPositionHistories.Add(head);
//             int i = 0;
//             while (i != subordinatesPositionHistories.Count)
//             {
//                 var subordinatesPositions = await _positions.Find(p => p.ParentId == subordinatesPositionHistories[i].PositionId).ToListAsync();
//                 var children = await _positionHistories.Find(ph => subordinatesPositions.Select(p => p.Id).ToList().Contains(ph.PositionId) && ph.EndDate == null).ToListAsync();
//                 subordinatesPositionHistories.AddRange(children);
//                 ++i;
//             }
//             var subordinatesId = subordinatesPositionHistories.Select(e => e.EmployeeId).ToList();
//             var filter = Builders<PostHistoryMongoDb>.Filter.In(ph => ph.EmployeeId, subordinatesId);
//             if (startDate.HasValue)
//                 filter = filter & Builders<PostHistoryMongoDb>.Filter.Gte(ph => ph.StartDate, startDate.Value.ToDateTime(TimeOnly.MinValue));
//             if (endDate.HasValue)
//                 filter = filter & Builders<PostHistoryMongoDb>.Filter.Lte(ph => ph.StartDate, endDate.Value.ToDateTime(TimeOnly.MinValue));
//             var postHistories = await _postHistories.Find(filter).ToListAsync();
//             var basePostHistories = postHistories.Select(PostHistoryConverter.ConvertMongo).ToList();
//             _logger.LogInformation("Got {Count} post histories for subordinates of manager {ManagerId}", basePostHistories.Count, managerId);
//             return basePostHistories;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error getting subordinate post histories for manager - {managerId}");
//             throw;
//         }
//     }
// }
