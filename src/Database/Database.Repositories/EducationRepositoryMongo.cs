// using Database.Context;
// using Database.Models;
// using Database.Models.Converters;
// using Microsoft.Extensions.Logging;
// using MongoDB.Driver;
// using Project.Core.Exceptions;
// using Project.Core.Models;
// using Project.Core.Models.Education;
// using Project.Core.Repositories;
//
// namespace Database.Repositories;
//
// public class EducationRepositoryMongo : IEducationRepository
// {
//     private readonly IMongoCollection<EducationMongoDb> _educations;
//     private readonly ILogger<EducationRepository> _logger;
//
//     public EducationRepositoryMongo(MongoDbContext context, ILogger<EducationRepository> logger)
//     {
//         _educations = context.Educations;
//         _logger = logger;
//         
//         // Создаем индексы для производительности
//         // CreateIndexes();
//     }
//
//     private void CreateIndexes()
//     {
//         // Составной индекс для проверки дубликатов
//         _educations.Indexes.CreateOne(
//             new CreateIndexModel<EducationMongoDb>(
//                 Builders<EducationMongoDb>.IndexKeys
//                     .Ascending(e => e.EmployeeId)
//                     .Ascending(e => e.Institution)
//                     .Ascending(e => e.StudyField)
//                     .Ascending(e => e.StartDate)
//                     .Ascending(e => e.EndDate),
//                 new CreateIndexOptions { Unique = true }
//             ));
//
//         // Индекс для поиска по EmployeeId
//         _educations.Indexes.CreateOne(
//             new CreateIndexModel<EducationMongoDb>(
//                 Builders<EducationMongoDb>.IndexKeys.Ascending(e => e.EmployeeId)
//             ));
//
//         // Индекс для сортировки по дате начала
//         _educations.Indexes.CreateOne(
//             new CreateIndexModel<EducationMongoDb>(
//                 Builders<EducationMongoDb>.IndexKeys.Ascending(e => e.StartDate)
//             ));
//     }
//
//     public async Task<BaseEducation> AddEducationAsync(CreateEducation education)
//     {
//         var educationDb = EducationConverter.ConvertMongo(education);
//         educationDb.CreatedAt = DateTime.UtcNow;
//         educationDb.UpdatedAt = DateTime.UtcNow;
//
//         try
//         {
//             // Проверка на существующее образование
//             var duplicateFilter = Builders<EducationMongoDb>.Filter.And(
//                 Builders<EducationMongoDb>.Filter.Eq(e => e.EmployeeId, educationDb.EmployeeId),
//                 Builders<EducationMongoDb>.Filter.Eq(e => e.Institution, educationDb.Institution),
//                 Builders<EducationMongoDb>.Filter.Eq(e => e.StudyField, educationDb.StudyField),
//                 Builders<EducationMongoDb>.Filter.Eq(e => e.StartDate, educationDb.StartDate),
//                 Builders<EducationMongoDb>.Filter.Eq(e => e.EndDate, educationDb.EndDate)
//             );
//
//             var existingEducation = await _educations.Find(duplicateFilter).FirstOrDefaultAsync();
//             if (existingEducation != null)
//             {
//                 _logger.LogWarning("Education already exists for employee {EmployeeId}", education.EmployeeId);
//                 throw new EducationAlreadyExistsException(
//                     $"Education already exists for employee {education.EmployeeId}");
//             }
//
//             await _educations.InsertOneAsync(educationDb);
//
//             _logger.LogInformation("Education with id {Id} was added for employee {EmployeeId}",
//                 educationDb.Id, education.EmployeeId);
//
//             return EducationConverter.ConvertMongo(educationDb);
//         }
//         catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
//         {
//             _logger.LogWarning(ex, "Education already exists for employee {EmployeeId}", education.EmployeeId);
//             throw new EducationAlreadyExistsException(
//                 $"Education already exists for employee {education.EmployeeId}");
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, "Error adding education for employee {EmployeeId}", education.EmployeeId);
//             throw;
//         }
//     }
//
//     public async Task<BaseEducation> GetEducationByIdAsync(Guid educationId)
//     {
//         try
//         {
//             var filter = Builders<EducationMongoDb>.Filter.Eq(e => e.Id, educationId);
//             var educationDb = await _educations.Find(filter).FirstOrDefaultAsync();
//             
//             if (educationDb == null)
//             {
//                 _logger.LogWarning("Education with id {Id} not found", educationId);
//                 throw new EducationNotFoundException($"Education with id {educationId} not found");
//             }
//
//             _logger.LogInformation("Education with id {Id} was found", educationId);
//             return EducationConverter.ConvertMongo(educationDb);
//         }
//         catch (Exception e) when (e is not EducationNotFoundException)
//         {
//             _logger.LogError(e, "Error getting education with id {Id}", educationId);
//             throw;
//         }
//     }
//
//     public async Task<BaseEducation> UpdateEducationAsync(UpdateEducation education)
//     {
//         try
//         {
//             // Поиск образования для обновления
//             var filter = Builders<EducationMongoDb>.Filter.And(
//                 Builders<EducationMongoDb>.Filter.Eq(e => e.Id, education.Id),
//                 Builders<EducationMongoDb>.Filter.Eq(e => e.EmployeeId, education.EmployeeId)
//             );
//
//             var educationDb = await _educations.Find(filter).FirstOrDefaultAsync();
//             if (educationDb == null)
//             {
//                 _logger.LogWarning("Education with id {Id} not found for update", education.Id);
//                 throw new EducationNotFoundException($"Education with id {education.Id} not found");
//             }
//
//             // Проверка на дубликат у других записей
//             var duplicateFilter = Builders<EducationMongoDb>.Filter.And(
//                 Builders<EducationMongoDb>.Filter.Ne(e => e.Id, education.Id),
//                 Builders<EducationMongoDb>.Filter.Eq(e => e.EmployeeId, education.EmployeeId),
//                 Builders<EducationMongoDb>.Filter.Eq(e => e.Institution, education.Institution ?? educationDb.Institution),
//                 Builders<EducationMongoDb>.Filter.Eq(e => e.StudyField, education.StudyField ?? educationDb.StudyField)
//             );
//
//             var existingEducation = await _educations.Find(duplicateFilter).FirstOrDefaultAsync();
//             if (existingEducation != null)
//             {
//                 _logger.LogWarning("Education record already exists for employee {EmployeeId}", education.EmployeeId);
//                 throw new EducationAlreadyExistsException("Education record already exists");
//             }
//
//             // Подготовка обновлений
//             var updateDefinition = Builders<EducationMongoDb>.Update
//                 .Set(e => e.UpdatedAt, DateTime.UtcNow);
//
//             if (education.Institution != null)
//                 updateDefinition = updateDefinition.Set(e => e.Institution, education.Institution);
//             if (education.StudyField != null)
//                 updateDefinition = updateDefinition.Set(e => e.StudyField, education.StudyField);
//             if (education.Level != null)
//                 updateDefinition = updateDefinition.Set(e => e.Level, education.Level.ToStringVal());
//             if (education.StartDate != null)
//                 updateDefinition = updateDefinition.Set(e => e.StartDate, education.StartDate.Value.ToDateTime(TimeOnly.MinValue));
//             if (education.EndDate != null)
//                 updateDefinition = updateDefinition.Set(e => e.EndDate, education.EndDate.Value.ToDateTime(TimeOnly.MinValue));
//
//             var options = new FindOneAndUpdateOptions<EducationMongoDb> 
//             { 
//                 ReturnDocument = ReturnDocument.After 
//             };
//
//             var updatedEducation = await _educations.FindOneAndUpdateAsync(
//                 filter, updateDefinition, options);
//
//             if (updatedEducation == null)
//                 throw new EducationNotFoundException($"Education with id {education.Id} not found after update");
//
//             _logger.LogInformation("Education with id {Id} was updated", education.Id);
//             return EducationConverter.ConvertMongo(updatedEducation);
//         }
//         catch (Exception e) when (e is not EducationNotFoundException)
//         {
//             _logger.LogError(e, "Error updating education with id {Id}", education.Id);
//             throw;
//         }
//     }
//
//     public async Task<IEnumerable<BaseEducation>> GetEducationsAsync(Guid employeeId, int pageNumber, int pageSize)
//     {
//         try
//         {
//             var filter = Builders<EducationMongoDb>.Filter.Eq(e => e.EmployeeId, employeeId);
//             var sort = Builders<EducationMongoDb>.Sort.Ascending(e => e.StartDate);
//
//             var educations = await _educations
//                 .Find(filter)
//                 .Sort(sort)
//                 .ToListAsync();
//
//             var baseEducations = educations.Select(e => EducationConverter.ConvertMongo(e)).ToList();
//
//             _logger.LogInformation("Got {Count} educations for employee {EmployeeId}",
//                 baseEducations.Count, employeeId);
//
//             return baseEducations;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, "Error getting educations for employee {EmployeeId}", employeeId);
//             throw;
//         }
//     }
//
//     public async Task DeleteEducationAsync(Guid educationId)
//     {
//         try
//         {
//             var filter = Builders<EducationMongoDb>.Filter.Eq(e => e.Id, educationId);
//             var result = await _educations.DeleteOneAsync(filter);
//
//             if (result.DeletedCount == 0)
//             {
//                 _logger.LogWarning("Education with id {Id} not found for deletion", educationId);
//                 throw new EducationNotFoundException($"Education with id {educationId} not found");
//             }
//
//             _logger.LogInformation("Education with id {Id} was deleted", educationId);
//         }
//         catch (Exception e) when (e is not EducationNotFoundException)
//         {
//             _logger.LogError(e, "Error deleting education with id {Id}", educationId);
//             throw;
//         }
//     }
// }