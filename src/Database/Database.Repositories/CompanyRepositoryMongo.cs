// using System.Data;
// using Database.Context;
// using Database.Models;
// using Database.Models.Converters;
// using Microsoft.Extensions.Logging;
// using MongoDB.Driver;
// using Project.Core.Exceptions;
// using Project.Core.Models;
// using Project.Core.Models.Company;
// using Project.Core.Repositories;
//
// namespace Database.Repositories;
//
// public class CompanyRepositoryMongo : ICompanyRepository
// {
//     private readonly IMongoCollection<CompanyMongoDb> _companies;
//     private readonly IMongoCollection<PostMongoDb> _posts;
//     private readonly IMongoCollection<PositionMongoDb> _positions;
//     private readonly ILogger<CompanyRepository> _logger;
//
//     public CompanyRepositoryMongo(MongoDbContext context, ILogger<CompanyRepository> logger)
//     {
//         _companies = context.Companies;
//         _posts = context.Posts;
//         _positions = context.Positions;
//         _logger = logger;
//         
//         // Создаем индексы для уникальности
//         // CreateIndexes();
//     }
//
//     private void CreateIndexes()
//     {
//         // Создаем уникальные индексы для проверки дубликатов
//         var options = new CreateIndexOptions { Unique = true };
//         
//         _companies.Indexes.CreateOne(
//             new CreateIndexModel<CompanyMongoDb>(
//                 Builders<CompanyMongoDb>.IndexKeys
//                     .Ascending(c => c.Title)
//                     .Ascending(c => c.IsDeleted),
//                 new CreateIndexOptions { Unique = true}
//             ));
//             
//         _companies.Indexes.CreateOne(
//             new CreateIndexModel<CompanyMongoDb>(
//                 Builders<CompanyMongoDb>.IndexKeys.Ascending(c => c.Inn),
//                 options));
//             
//         _companies.Indexes.CreateOne(
//             new CreateIndexModel<CompanyMongoDb>(
//                 Builders<CompanyMongoDb>.IndexKeys.Ascending(c => c.Ogrn),
//                 options));
//     }
//
//     public async Task<BaseCompany> AddCompanyAsync(CreationCompany newCompany)
//     {
//         var company = CompanyConverter.ConvertMongo(newCompany);
//
//         try
//         {
//             // Проверка на существование компании с такими же данными
//             var existingCompanyFilter = Builders<CompanyMongoDb>.Filter.And(
//                 Builders<CompanyMongoDb>.Filter.Eq(c => c.IsDeleted, false),
//                 Builders<CompanyMongoDb>.Filter.Or(
//                     Builders<CompanyMongoDb>.Filter.Eq(c => c.Title, company.Title),
//                     Builders<CompanyMongoDb>.Filter.Eq(c => c.PhoneNumber, company.PhoneNumber),
//                     Builders<CompanyMongoDb>.Filter.Eq(c => c.Email, company.Email),
//                     Builders<CompanyMongoDb>.Filter.Eq(c => c.Inn, company.Inn),
//                     Builders<CompanyMongoDb>.Filter.Eq(c => c.Kpp, company.Kpp),
//                     Builders<CompanyMongoDb>.Filter.Eq(c => c.Ogrn, company.Ogrn)
//                 )
//             );
//
//             var existingCompany = await _companies.Find(existingCompanyFilter).FirstOrDefaultAsync();
//             if (existingCompany != null)
//                 throw new CompanyAlreadyExistsException(
//                     $"Company with same title - {company.Title} or phone - {company.PhoneNumber} or email - {company.Email} or inn - {company.Inn} or kpp - {company.Kpp} or ogrn - {company.Ogrn} already exists");
//
//             await _companies.InsertOneAsync(company);
//             return CompanyConverter.ConvertMongo(company);
//         }
//         catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
//         {
//             _logger.LogError(ex, $"Duplicate key error creating company with id - {company.Id}");
//             throw new CompanyAlreadyExistsException("Company with duplicate unique fields already exists");
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error creating company with id - {company.Id}");
//             throw;
//         }
//     }
//
//     public async Task<BaseCompany> UpdateCompanyAsync(UpdateCompany company)
//     {
//         try
//         {
//             // Проверка на дубликаты у других компаний
//             var duplicateFilter = Builders<CompanyMongoDb>.Filter.And(
//                 Builders<CompanyMongoDb>.Filter.Eq(c => c.IsDeleted, false),
//                 Builders<CompanyMongoDb>.Filter.Ne(c => c.Id, company.CompanyId),
//                 Builders<CompanyMongoDb>.Filter.Or(
//                     Builders<CompanyMongoDb>.Filter.Eq(c => c.Title, company.Title),
//                     Builders<CompanyMongoDb>.Filter.Eq(c => c.PhoneNumber, company.PhoneNumber),
//                     Builders<CompanyMongoDb>.Filter.Eq(c => c.Email, company.Email),
//                     Builders<CompanyMongoDb>.Filter.Eq(c => c.Inn, company.Inn),
//                     Builders<CompanyMongoDb>.Filter.Eq(c => c.Kpp, company.Kpp),
//                     Builders<CompanyMongoDb>.Filter.Eq(c => c.Ogrn, company.Ogrn)
//                 )
//             );
//
//             var duplicateCount = await _companies.CountDocumentsAsync(duplicateFilter);
//             if (duplicateCount > 0)
//                 throw new CompanyAlreadyExistsException(
//                     $"Company with another id, but same title - {company.Title} or phone - {company.PhoneNumber} or email - {company.Email} or inn - {company.Inn} or kpp - {company.Kpp} or ogrn - {company.Ogrn} already exists");
//
//             // Поиск компании для обновления
//             var companyFilter = Builders<CompanyMongoDb>.Filter.And(
//                 Builders<CompanyMongoDb>.Filter.Eq(c => c.Id, company.CompanyId),
//                 Builders<CompanyMongoDb>.Filter.Eq(c => c.IsDeleted, false)
//             );
//
//             var companyToUpdate = await _companies.Find(companyFilter).FirstOrDefaultAsync();
//             if (companyToUpdate == null)
//                 throw new CompanyNotFoundException($"Company with id {company.CompanyId} not found");
//
//             // Подготовка обновлений
//             var updateDefinition = Builders<CompanyMongoDb>.Update
//                 .Set(c => c.UpdatedAt, DateTime.UtcNow);
//
//             if (company.Title != null) 
//                 updateDefinition = updateDefinition.Set(c => c.Title, company.Title);
//             if (company.RegistrationDate.HasValue) 
//                 updateDefinition = updateDefinition.Set(c => c.RegistrationDate, company.RegistrationDate.Value.ToDateTime(TimeOnly.MinValue));
//             if (company.PhoneNumber != null) 
//                 updateDefinition = updateDefinition.Set(c => c.PhoneNumber, company.PhoneNumber);
//             if (company.Email != null) 
//                 updateDefinition = updateDefinition.Set(c => c.Email, company.Email);
//             if (company.Inn != null) 
//                 updateDefinition = updateDefinition.Set(c => c.Inn, company.Inn);
//             if (company.Kpp != null) 
//                 updateDefinition = updateDefinition.Set(c => c.Kpp, company.Kpp);
//             if (company.Ogrn != null) 
//                 updateDefinition = updateDefinition.Set(c => c.Ogrn, company.Ogrn);
//             if (company.Address != null) 
//                 updateDefinition = updateDefinition.Set(c => c.Address, company.Address);
//
//             var options = new FindOneAndUpdateOptions<CompanyMongoDb> 
//             { 
//                 ReturnDocument = ReturnDocument.After 
//             };
//
//             var updatedCompany = await _companies.FindOneAndUpdateAsync(
//                 companyFilter, updateDefinition, options);
//
//             if (updatedCompany == null)
//                 throw new CompanyNotFoundException($"Company with id {company.CompanyId} not found after update");
//
//             return CompanyConverter.ConvertMongo(updatedCompany);
//         }
//         catch (CompanyNotFoundException e)
//         {
//             _logger.LogWarning(e, $"Company with id {company.CompanyId} not found");
//             throw;
//         }
//         catch (CompanyAlreadyExistsException e)
//         {
//             _logger.LogWarning(e, "Company with another id, but same unique fields already exists");
//             throw;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error updating company with id - {company.CompanyId}");
//             throw;
//         }
//     }
//
//     public async Task<BaseCompany> GetCompanyByIdAsync(Guid companyId)
//     {
//         try
//         {
//             var filter = Builders<CompanyMongoDb>.Filter.And(
//                 Builders<CompanyMongoDb>.Filter.Eq(c => c.Id, companyId),
//                 Builders<CompanyMongoDb>.Filter.Eq(c => c.IsDeleted, false)
//             );
//
//             var company = await _companies.Find(filter).FirstOrDefaultAsync();
//             if (company == null)
//                 throw new CompanyNotFoundException($"Company with id {companyId} not found");
//
//             return CompanyConverter.ConvertMongo(company);
//         }
//         catch (CompanyNotFoundException e)
//         {
//             _logger.LogWarning(e, $"Company with id {companyId} not found");
//             throw;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error getting company with id - {companyId}");
//             throw;
//         }
//     }
//
//     public async Task DeleteCompanyAsync(Guid companyId)
//     {
//         var session = await _companies.Database.Client.StartSessionAsync();
//         session.StartTransaction();
//
//         try
//         {
//             // 1. Проверяем существование компании
//             var companyFilter = Builders<CompanyMongoDb>.Filter.And(
//                 Builders<CompanyMongoDb>.Filter.Eq(c => c.Id, companyId),
//                 Builders<CompanyMongoDb>.Filter.Eq(c => c.IsDeleted, false)
//             );
//
//             var company = await _companies.Find(companyFilter).FirstOrDefaultAsync();
//             if (company == null)
//                 throw new CompanyNotFoundException($"Company with id {companyId} not found");
//
//             // 2. Помечаем компанию как удаленную
//             var companyUpdate = Builders<CompanyMongoDb>.Update
//                 .Set(c => c.IsDeleted, true)
//                 .Set(c => c.UpdatedAt, DateTime.UtcNow);
//
//             await _companies.UpdateOneAsync(session, companyFilter, companyUpdate);
//
//             // 3. Помечаем посты как удаленные
//             var postFilter = Builders<PostMongoDb>.Filter.Eq(p => p.CompanyId, companyId);
//             var postUpdate = Builders<PostMongoDb>.Update
//                 .Set(p => p.IsDeleted, true)
//                 .Set(p => p.UpdatedAt, DateTime.UtcNow);
//
//             await _posts.UpdateManyAsync(session, postFilter, postUpdate);
//
//             // 4. Помечаем позиции как удаленные
//             var positionFilter = Builders<PositionMongoDb>.Filter.Eq(p => p.CompanyId, companyId);
//             var positionUpdate = Builders<PositionMongoDb>.Update
//                 .Set(p => p.IsDeleted, true)
//                 .Set(p => p.UpdatedAt, DateTime.UtcNow);
//
//             await _positions.UpdateManyAsync(session, positionFilter, positionUpdate);
//
//             // 5. Для PostHistory и PositionHistory нужно добавить аналогичную логику
//             // (предполагается, что у вас есть соответствующие коллекции)
//
//             await session.CommitTransactionAsync();
//         }
//         catch (CompanyNotFoundException e)
//         {
//             await session.AbortTransactionAsync();
//             _logger.LogWarning(e, $"Company with id {companyId} not found for deleting");
//         }
//         catch (Exception e)
//         {
//             await session.AbortTransactionAsync();
//             _logger.LogError(e, $"Error deleting company with id - {companyId}");
//             throw;
//         }
//         finally
//         {
//             session.Dispose();
//         }
//     }
//
//     public async Task<IEnumerable<BaseCompany>> GetCompaniesAsync()
//     {
//         try
//         {
//             var filter = Builders<CompanyMongoDb>.Filter.Eq(c => c.IsDeleted, false);
//             var sort = Builders<CompanyMongoDb>.Sort.Ascending(c => c.RegistrationDate);
//
//
//             var companies = await _companies
//                 .Find(filter)
//                 .Sort(sort)
//                 .ToListAsync();
//
//             var baseCompanies = companies.Select(c=>CompanyConverter.ConvertMongo(c)).ToList();
//
//             return baseCompanies;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, "Error getting companies");
//             throw;
//         }
//     }
// }