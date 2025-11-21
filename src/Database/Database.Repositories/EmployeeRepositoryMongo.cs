// using Database.Context;
// using Database.Models;
// using Database.Models.Converters;
// using Microsoft.Extensions.Logging;
// using MongoDB.Driver;
// using Project.Core.Exceptions;
// using Project.Core.Models;
// using Project.Core.Models.Employee;
// using Project.Core.Repositories;
//
// namespace Database.Repositories;
//
// public class EmployeeRepositoryMongo : IEmployeeRepository
// {
//     private readonly IMongoCollection<EmployeeMongoDb> _employees;
//     private readonly ILogger<EmployeeRepository> _logger;
//
//     public EmployeeRepositoryMongo(MongoDbContext context, ILogger<EmployeeRepository> logger)
//     {
//         _employees = context.Employees;
//         _logger = logger;
//         
//         // Создаем индексы для производительности
//         // CreateIndexes();
//     }
//
//     private void CreateIndexes()
//     {
//         // Уникальный индекс для email
//         _employees.Indexes.CreateOne(
//             new CreateIndexModel<EmployeeMongoDb>(
//                 Builders<EmployeeMongoDb>.IndexKeys.Ascending(e => e.Email),
//                 new CreateIndexOptions { Unique = true }
//             ));
//
//         // Индекс для поиска по телефону
//         _employees.Indexes.CreateOne(
//             new CreateIndexModel<EmployeeMongoDb>(
//                 Builders<EmployeeMongoDb>.IndexKeys.Ascending(e => e.Phone)
//             ));
//
//         // Индекс для сортировки по имени
//         _employees.Indexes.CreateOne(
//             new CreateIndexModel<EmployeeMongoDb>(
//                 Builders<EmployeeMongoDb>.IndexKeys.Ascending(e => e.FullName)
//             ));
//     }
//
//     public async Task<BaseEmployee> AddEmployeeAsync(CreationEmployee newEmployee)
//     {
//         var employee = EmployeeConverter.ConvertMongo(newEmployee);
//         employee.CreatedAt = DateTime.UtcNow;
//         employee.UpdatedAt = DateTime.UtcNow;
//
//         try
//         {
//             // Проверка на существующего сотрудника с таким же email
//             var existingEmployeeFilter = Builders<EmployeeMongoDb>.Filter.Eq(e => e.Email, employee.Email);
//             var existingEmployee = await _employees.Find(existingEmployeeFilter).FirstOrDefaultAsync();
//             
//             if (existingEmployee != null)
//                 throw new EmployeeAlreadyExistsException($"Employee with email {employee.Email} already exists");
//
//             await _employees.InsertOneAsync(employee);
//             
//             _logger.LogInformation("Employee with id {Id} was added", employee.Id);
//             return EmployeeConverter.ConvertMongo(employee);
//         }
//         catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
//         {
//             _logger.LogError(ex, $"Duplicate key error creating employee with email - {employee.Email}");
//             throw new EmployeeAlreadyExistsException($"Employee with email {employee.Email} already exists");
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error creating employee with email - {employee.Email}");
//             throw;
//         }
//     }
//
//     public async Task<BaseEmployee> UpdateEmployeeAsync(UpdateEmployee updateEmployee)
//     {
//         try
//         {
//             // Проверка на дубликаты у других сотрудников
//             var duplicateFilter = Builders<EmployeeMongoDb>.Filter.And(
//                 Builders<EmployeeMongoDb>.Filter.Ne(e => e.Id, updateEmployee.EmployeeId),
//                 Builders<EmployeeMongoDb>.Filter.Or(
//                     Builders<EmployeeMongoDb>.Filter.Eq(e => e.Email, updateEmployee.Email),
//                     Builders<EmployeeMongoDb>.Filter.Eq(e => e.Phone, updateEmployee.PhoneNumber)
//                 )
//             );
//
//             var duplicateCount = await _employees.CountDocumentsAsync(duplicateFilter);
//             if (duplicateCount > 0)
//                 throw new EmployeeAlreadyExistsException(
//                     $"Employee with another id, but same email - {updateEmployee.Email} or phone - {updateEmployee.PhoneNumber} already exists");
//
//             // Поиск сотрудника для обновления
//             var employeeFilter = Builders<EmployeeMongoDb>.Filter.Eq(e => e.Id, updateEmployee.EmployeeId);
//             var employeeToUpdate = await _employees.Find(employeeFilter).FirstOrDefaultAsync();
//             
//             if (employeeToUpdate == null)
//                 throw new EmployeeNotFoundException($"Employee with id {updateEmployee.EmployeeId} not found");
//
//             // Подготовка обновлений
//             var updateDefinition = Builders<EmployeeMongoDb>.Update
//                 .Set(e => e.UpdatedAt, DateTime.UtcNow);
//
//             if (updateEmployee.FullName != null) 
//                 updateDefinition = updateDefinition.Set(e => e.FullName, updateEmployee.FullName);
//             if (updateEmployee.PhoneNumber != null) 
//                 updateDefinition = updateDefinition.Set(e => e.Phone, updateEmployee.PhoneNumber);
//             if (updateEmployee.Email != null) 
//                 updateDefinition = updateDefinition.Set(e => e.Email, updateEmployee.Email);
//             if (updateEmployee.BirthDate.HasValue) 
//                 updateDefinition = updateDefinition.Set(e => e.BirthDate, updateEmployee.BirthDate.Value.ToDateTime(TimeOnly.MinValue));
//             if (updateEmployee.Photo != null) 
//                 updateDefinition = updateDefinition.Set(e => e.Photo, updateEmployee.Photo);
//             if (updateEmployee.Duties != null) 
//                 updateDefinition = updateDefinition.Set(e => e.Duties, updateEmployee.Duties);
//
//             var options = new FindOneAndUpdateOptions<EmployeeMongoDb> 
//             { 
//                 ReturnDocument = ReturnDocument.After 
//             };
//
//             var updatedEmployee = await _employees.FindOneAndUpdateAsync(
//                 employeeFilter, updateDefinition, options);
//
//             if (updatedEmployee == null)
//                 throw new EmployeeNotFoundException($"Employee with id {updateEmployee.EmployeeId} not found after update");
//
//             _logger.LogInformation("Employee with id {Id} was updated", updateEmployee.EmployeeId);
//             return EmployeeConverter.ConvertMongo(updatedEmployee);
//         }
//         catch (EmployeeNotFoundException e)
//         {
//             _logger.LogWarning(e, $"Employee with id {updateEmployee.EmployeeId} not found");
//             throw;
//         }
//         catch (EmployeeAlreadyExistsException e)
//         {
//             _logger.LogWarning(e, "Employee with another id, but same unique fields already exists");
//             throw;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error updating employee with id - {updateEmployee.EmployeeId}");
//             throw;
//         }
//     }
//
//     public async Task<BaseEmployee> GetEmployeeByIdAsync(Guid employeeId)
//     {
//         try
//         {
//             var filter = Builders<EmployeeMongoDb>.Filter.Eq(e => e.Id, employeeId);
//             var employee = await _employees.Find(filter).FirstOrDefaultAsync();
//             
//             if (employee == null)
//                 throw new EmployeeNotFoundException($"Employee with id {employeeId} not found");
//
//             _logger.LogInformation("Employee with id {Id} was found", employeeId);
//             return EmployeeConverter.ConvertMongo(employee);
//         }
//         catch (EmployeeNotFoundException e)
//         {
//             _logger.LogWarning(e, $"Employee with id {employeeId} not found");
//             throw;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error getting employee with id - {employeeId}");
//             throw;
//         }
//     }
//
//     public async Task<IEnumerable<BaseEmployee>> GetSubordinatesByDirectorIdAsync(Guid directorId)
//     {
//         try
//         {
//             // В MongoDB версии нужно будет реализовать логику получения подчиненных
//             // Это может потребовать дополнительных коллекций или агрегаций
//             // Пока возвращаем пустой список
//             _logger.LogInformation("Getting subordinates for director {DirectorId}", directorId);
//             return new List<BaseEmployee>();
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error getting subordinates for director - {directorId}");
//             throw;
//         }
//     }
//
//     public async Task DeleteEmployeeAsync(Guid employeeId)
//     {
//         try
//         {
//             var filter = Builders<EmployeeMongoDb>.Filter.Eq(e => e.Id, employeeId);
//             var result = await _employees.DeleteOneAsync(filter);
//
//             if (result.DeletedCount == 0)
//             {
//                 _logger.LogWarning("Employee with id {Id} not found for deletion", employeeId);
//                 throw new EmployeeNotFoundException($"Employee with id {employeeId} not found");
//             }
//
//             _logger.LogInformation("Employee with id {Id} was deleted", employeeId);
//         }
//         catch (EmployeeNotFoundException e)
//         {
//             _logger.LogWarning(e, $"Employee with id {employeeId} not found for deletion");
//             throw;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error deleting employee with id - {employeeId}");
//             throw;
//         }
//     }
// }
