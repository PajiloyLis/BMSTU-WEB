// using Database.Context;
// using Database.Models;
// using Database.Models.Converters;
// using Microsoft.Extensions.Logging;
// using MongoDB.Driver;
// using Project.Core.Exceptions;
// using Project.Core.Models.PositionHistory;
// using Project.Core.Repositories;
// using Project.Database.Models;
// using Project.Database.Models.Converters;
// using Project.Database.Repositories;
//
// namespace Database.Repositories;
//
// public class PositionHistoryRepositoryMongo : IPositionHistoryRepository
// {
//     private readonly IMongoCollection<PositionHistoryMongoDb> _positionHistories;
//     private readonly IMongoCollection<PositionMongoDb> _positions;
//     private readonly ILogger<PositionHistoryRepository> _logger;
//
//     public PositionHistoryRepositoryMongo(MongoDbContext context, ILogger<PositionHistoryRepository> logger)
//     {
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
//         _positionHistories.Indexes.CreateOne(
//             new CreateIndexModel<PositionHistoryMongoDb>(
//                 Builders<PositionHistoryMongoDb>.IndexKeys
//                     .Ascending(ph => ph.PositionId)
//                     .Ascending(ph => ph.EmployeeId)
//                     .Ascending(ph => ph.StartDate),
//                 new CreateIndexOptions { Unique = true }
//             ));
//
//         // Индекс для поиска по EmployeeId
//         _positionHistories.Indexes.CreateOne(
//             new CreateIndexModel<PositionHistoryMongoDb>(
//                 Builders<PositionHistoryMongoDb>.IndexKeys.Ascending(ph => ph.EmployeeId)
//             ));
//
//         // Индекс для поиска по PositionId
//         _positionHistories.Indexes.CreateOne(
//             new CreateIndexModel<PositionHistoryMongoDb>(
//                 Builders<PositionHistoryMongoDb>.IndexKeys.Ascending(ph => ph.PositionId)
//             ));
//
//         // Индекс для сортировки по дате начала
//         _positionHistories.Indexes.CreateOne(
//             new CreateIndexModel<PositionHistoryMongoDb>(
//                 Builders<PositionHistoryMongoDb>.IndexKeys.Ascending(ph => ph.StartDate)
//             ));
//     }
//
//     public async Task<BasePositionHistory> AddPositionHistoryAsync(CreatePositionHistory createPositionHistory)
//     {
//         var positionHistoryDb = PositionHistoryConverter.ConvertMongo(createPositionHistory);
//         positionHistoryDb.CreatedAt = DateTime.UtcNow;
//         positionHistoryDb.UpdatedAt = DateTime.UtcNow;
//
//         try
//         {
//             // Проверка на существующую запись истории
//             var duplicateFilter = Builders<PositionHistoryMongoDb>.Filter.And(
//                 Builders<PositionHistoryMongoDb>.Filter.Eq(ph => ph.PositionId, positionHistoryDb.PositionId),
//                 Builders<PositionHistoryMongoDb>.Filter.Eq(ph => ph.EmployeeId, positionHistoryDb.EmployeeId),
//                 Builders<PositionHistoryMongoDb>.Filter.Eq(ph => ph.StartDate, positionHistoryDb.StartDate)
//             );
//
//             var existingPositionHistory = await _positionHistories.Find(duplicateFilter).FirstOrDefaultAsync();
//             if (existingPositionHistory != null)
//             {
//                 _logger.LogWarning("Position history already exists for position {PositionId} and employee {EmployeeId}", 
//                     createPositionHistory.PositionId, createPositionHistory.EmployeeId);
//                 throw new PositionHistoryNotFoundException(
//                     $"Position history already exists for position {createPositionHistory.PositionId} and employee {createPositionHistory.EmployeeId}");
//             }
//
//             await _positionHistories.InsertOneAsync(positionHistoryDb);
//
//             _logger.LogInformation("Position history for position {PositionId} and employee {EmployeeId} was added", 
//                 positionHistoryDb.PositionId, positionHistoryDb.EmployeeId);
//             return PositionHistoryConverter.ConvertMongo(positionHistoryDb);
//         }
//         catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
//         {
//             _logger.LogWarning(ex, "Position history already exists for position {PositionId} and employee {EmployeeId}", 
//                 createPositionHistory.PositionId, createPositionHistory.EmployeeId);
//             throw new PositionHistoryNotFoundException(
//                 $"Position history already exists for position {createPositionHistory.PositionId} and employee {createPositionHistory.EmployeeId}");
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, "Error adding position history for position {PositionId} and employee {EmployeeId}", 
//                 createPositionHistory.PositionId, createPositionHistory.EmployeeId);
//             throw;
//         }
//     }
//
//     public async Task<BasePositionHistory> GetPositionHistoryByIdAsync(Guid positionId, Guid employeeId)
//     {
//         try
//         {
//             var filter = Builders<PositionHistoryMongoDb>.Filter.And(
//                 Builders<PositionHistoryMongoDb>.Filter.Eq(ph => ph.PositionId, positionId),
//                 Builders<PositionHistoryMongoDb>.Filter.Eq(ph => ph.EmployeeId, employeeId)
//             );
//             
//             var positionHistory = await _positionHistories.Find(filter).FirstOrDefaultAsync();
//             
//             if (positionHistory == null)
//             {
//                 _logger.LogWarning("Position history for position {PositionId} and employee {EmployeeId} not found", positionId, employeeId);
//                 throw new PositionHistoryNotFoundException(
//                     $"Position history for position {positionId} and employee {employeeId} not found");
//             }
//
//             _logger.LogInformation("Position history for position {PositionId} and employee {EmployeeId} was found", positionId, employeeId);
//             return PositionHistoryConverter.ConvertMongo(positionHistory);
//         }
//         catch (PositionHistoryNotFoundException e)
//         {
//             _logger.LogWarning(e, $"Position history for position {positionId} and employee {employeeId} not found");
//             throw;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error getting position history for position {positionId} and employee {employeeId}");
//             throw;
//         }
//     }
//
//     public async Task<BasePositionHistory> UpdatePositionHistoryAsync(UpdatePositionHistory updatePositionHistory)
//     {
//         try
//         {
//             var filter = Builders<PositionHistoryMongoDb>.Filter.And(
//                 Builders<PositionHistoryMongoDb>.Filter.Eq(ph => ph.PositionId, updatePositionHistory.PositionId),
//                 Builders<PositionHistoryMongoDb>.Filter.Eq(ph => ph.EmployeeId, updatePositionHistory.EmployeeId)
//             );
//
//             var positionHistoryToUpdate = await _positionHistories.Find(filter).FirstOrDefaultAsync();
//             if (positionHistoryToUpdate == null)
//                 throw new PositionHistoryNotFoundException(
//                     $"Position history for position {updatePositionHistory.PositionId} and employee {updatePositionHistory.EmployeeId} not found");
//
//             // Подготовка обновлений
//             var updateDefinition = Builders<PositionHistoryMongoDb>.Update
//                 .Set(ph => ph.UpdatedAt, DateTime.UtcNow);
//
//             if (updatePositionHistory.StartDate.HasValue)
//                 updateDefinition = updateDefinition.Set(ph => ph.StartDate, updatePositionHistory.StartDate.Value.ToDateTime(TimeOnly.MinValue));
//             if (updatePositionHistory.EndDate.HasValue)
//                 updateDefinition = updateDefinition.Set(ph => ph.EndDate, updatePositionHistory.EndDate.Value.ToDateTime(TimeOnly.MinValue));
//
//             var options = new FindOneAndUpdateOptions<PositionHistoryMongoDb> 
//             { 
//                 ReturnDocument = ReturnDocument.After 
//             };
//
//             var updatedPositionHistory = await _positionHistories.FindOneAndUpdateAsync(
//                 filter, updateDefinition, options);
//
//             if (updatedPositionHistory == null)
//                 throw new PositionHistoryNotFoundException(
//                     $"Position history for position {updatePositionHistory.PositionId} and employee {updatePositionHistory.EmployeeId} not found after update");
//
//             _logger.LogInformation("Position history for position {PositionId} and employee {EmployeeId} was updated", 
//                 updatePositionHistory.PositionId, updatePositionHistory.EmployeeId);
//             return PositionHistoryConverter.ConvertMongo(updatedPositionHistory);
//         }
//         catch (PositionHistoryNotFoundException e)
//         {
//             _logger.LogWarning(e, $"Position history for position {updatePositionHistory.PositionId} and employee {updatePositionHistory.EmployeeId} not found");
//             throw;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error updating position history for position {updatePositionHistory.PositionId} and employee {updatePositionHistory.EmployeeId}");
//             throw;
//         }
//     }
//
//     public async Task DeletePositionHistoryAsync(Guid positionId, Guid employeeId)
//     {
//         try
//         {
//             var filter = Builders<PositionHistoryMongoDb>.Filter.And(
//                 Builders<PositionHistoryMongoDb>.Filter.Eq(ph => ph.PositionId, positionId),
//                 Builders<PositionHistoryMongoDb>.Filter.Eq(ph => ph.EmployeeId, employeeId)
//             );
//
//             var result = await _positionHistories.DeleteOneAsync(filter);
//
//             if (result.DeletedCount == 0)
//             {
//                 _logger.LogWarning("Position history for position {PositionId} and employee {EmployeeId} not found for deletion", positionId, employeeId);
//                 throw new PositionHistoryNotFoundException(
//                     $"Position history for position {positionId} and employee {employeeId} not found");
//             }
//
//             _logger.LogInformation("Position history for position {PositionId} and employee {EmployeeId} was deleted", positionId, employeeId);
//         }
//         catch (PositionHistoryNotFoundException e)
//         {
//             _logger.LogWarning(e, $"Position history for position {positionId} and employee {employeeId} not found for deletion");
//             throw;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error deleting position history for position {positionId} and employee {employeeId}");
//             throw;
//         }
//     }
//
//     public async Task<IEnumerable<BasePositionHistory>> GetPositionHistoryByEmployeeIdAsync(Guid employeeId,
//         DateOnly? startDate,
//         DateOnly? endDate)
//     {
//         try
//         {
//             var filterBuilder = Builders<PositionHistoryMongoDb>.Filter;
//             var filter = filterBuilder.Eq(ph => ph.EmployeeId, employeeId);
//
//             if (startDate.HasValue)
//                 filter = filter & filterBuilder.Gte(ph => ph.StartDate, startDate.Value.ToDateTime(TimeOnly.MinValue));
//             if (endDate.HasValue)
//                 filter = filter & filterBuilder.Lte(ph => ph.StartDate, endDate.Value.ToDateTime(TimeOnly.MinValue));
//
//             var sort = Builders<PositionHistoryMongoDb>.Sort.Descending(ph => ph.StartDate);
//
//             var positionHistories = await _positionHistories
//                 .Find(filter)
//                 .Sort(sort)
//                 .ToListAsync();
//
//             var basePositionHistories = positionHistories.Select(PositionHistoryConverter.ConvertMongo).ToList();
//
//             _logger.LogInformation("Got {Count} position histories for employee {EmployeeId}", basePositionHistories.Count, employeeId);
//             return basePositionHistories;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error getting position histories for employee - {employeeId}");
//             throw;
//         }
//     }
//
//     public async Task<IEnumerable<PositionHierarchyWithEmployee>> GetCurrentSubordinatesAsync(Guid managerId)
//     {
//         try
//         {
//             var subordinates = await GetAllCurrentSubordinates(managerId);
//             _logger.LogInformation("Getting current subordinates for manager {ManagerId}", managerId);
//             return subordinates;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error getting current subordinates for manager - {managerId}");
//             throw;
//         }
//     }
//
//     public async Task<IEnumerable<BasePositionHistory>> GetCurrentSubordinatesPositionHistoryAsync(Guid managerId,
//         DateOnly? startDate, DateOnly? endDate)
//     {
//         try
//         { 
//             _logger.LogInformation("Getting current subordinates position history for manager {ManagerId}", managerId); 
//             var subordinates = await GetAllCurrentSubordinates(managerId);
//             var employees = subordinates.Select(e => e.EmployeeId).ToList();
//             var filter = Builders<PositionHistoryMongoDb>.Filter.In(ph => ph.EmployeeId, employees);
//             if (startDate.HasValue)
//                 filter = filter & Builders<PositionHistoryMongoDb>.Filter.Gte(ph => ph.StartDate, startDate.Value.ToDateTime(TimeOnly.MinValue));
//             if (endDate.HasValue)
//                 filter = filter & Builders<PositionHistoryMongoDb>.Filter.Lte(ph => ph.StartDate, endDate.Value.ToDateTime(TimeOnly.MinValue));
//             var positionHistories = await _positionHistories.Find(filter).ToListAsync();
//             var basePositionHistories = positionHistories.Select(PositionHistoryConverter.ConvertMongo).ToList();
//             _logger.LogInformation("Got {Count} position histories for current subordinates of manager {ManagerId}", basePositionHistories.Count, managerId);
//            return basePositionHistories;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error getting current subordinates position history for manager - {managerId}");
//             throw;
//         }
//     }
//
//     private async Task<List<PositionHierarchyWithEmployee>> GetAllCurrentSubordinates(Guid managerId)
//     {
//         try
//         {
//             List<PositionHierarchyWithEmployee> subordinates = new List<PositionHierarchyWithEmployee>();
//             var head = await _positionHistories.Find(ph => ph.EmployeeId == managerId && ph.EndDate == null).FirstOrDefaultAsync();
//             if (head is null)
//                 throw new PositionHistoryNotFoundException($"Current position for employee {managerId} not found");
//             var headPosition = await _positions.Find(p => p.Id == head.PositionId).FirstOrDefaultAsync();
//             subordinates.Add(new PositionHierarchyWithEmployee(head.EmployeeId, head.PositionId, headPosition.ParentId, headPosition.Title, 0));
//             int i = 0;
//             while (i != subordinates.Count)
//             {
//                 var subordinatesPositions = await _positions.Find(p => p.ParentId == subordinates[i].PositionId).ToListAsync();
//                 var children = await _positionHistories.Find(ph => subordinatesPositions.Select(p => p.Id).ToList().Contains(ph.PositionId) && ph.EndDate == null).ToListAsync();
//                 var resultChildren = children.Select(e => new PositionHierarchyWithEmployee(e.EmployeeId, e.PositionId, subordinatesPositions.Where(p => p.Id == e.PositionId).FirstOrDefault().ParentId, subordinatesPositions.Where(p => p.Id == e.PositionId).FirstOrDefault().Title, subordinates[i].Level + 1));
//                 subordinates.AddRange(resultChildren);
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
