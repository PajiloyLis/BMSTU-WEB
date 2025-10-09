using Database.Context;
using Database.Models;
using Database.Models.Converters;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Project.Core.Exceptions;
using Project.Core.Models;
using Project.Core.Models.Position;
using Project.Core.Models.PositionHistory;
using Project.Core.Repositories;

namespace Database.Repositories;

public class PositionRepositoryMongo : IPositionRepository
{
    private readonly IMongoCollection<PositionMongoDb> _positions;
    private readonly ILogger<PositionRepository> _logger;

    public PositionRepositoryMongo(MongoDbContext context, ILogger<PositionRepository> logger)
    {
        _positions = context.Positions;
        _logger = logger;
        
        // Создаем индексы для производительности
        // CreateIndexes();
    }

    private void CreateIndexes()
    {
        // Составной индекс для проверки дубликатов
        _positions.Indexes.CreateOne(
            new CreateIndexModel<PositionMongoDb>(
                Builders<PositionMongoDb>.IndexKeys
                    .Ascending(p => p.CompanyId)
                    .Ascending(p => p.Title)
                    .Ascending(p => p.IsDeleted),
                new CreateIndexOptions { Unique = true }
            ));

        // Индекс для поиска по CompanyId
        _positions.Indexes.CreateOne(
            new CreateIndexModel<PositionMongoDb>(
                Builders<PositionMongoDb>.IndexKeys.Ascending(p => p.CompanyId)
            ));

        // Индекс для поиска по ParentId
        _positions.Indexes.CreateOne(
            new CreateIndexModel<PositionMongoDb>(
                Builders<PositionMongoDb>.IndexKeys.Ascending(p => p.ParentId)
            ));
    }

    public async Task<BasePosition> AddPositionAsync(CreatePosition position)
    {
        var positionDb = PositionConverter.ConvertMongo(position);
        positionDb.CreatedAt = DateTime.UtcNow;
        positionDb.UpdatedAt = DateTime.UtcNow;

        try
        {
            // Проверка на существующую позицию
            var duplicateFilter = Builders<PositionMongoDb>.Filter.And(
                Builders<PositionMongoDb>.Filter.Eq(p => p.CompanyId, positionDb.CompanyId),
                Builders<PositionMongoDb>.Filter.Eq(p => p.Title, positionDb.Title),
                Builders<PositionMongoDb>.Filter.Eq(p => p.IsDeleted, false)
            );

            var existingPosition = await _positions.Find(duplicateFilter).FirstOrDefaultAsync();
            if (existingPosition != null)
            {
                _logger.LogWarning("Position already exists in company {CompanyId}", position.CompanyId);
                throw new PositionAlreadyExistsException(
                    $"Position with title {position.Title} already exists in company {position.CompanyId}");
            }

            await _positions.InsertOneAsync(positionDb);

            _logger.LogInformation("Position with id {Id} was added", positionDb.Id);
            return PositionConverter.ConvertMongo(positionDb);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            _logger.LogWarning(ex, "Position already exists in company {CompanyId}", position.CompanyId);
            throw new PositionAlreadyExistsException(
                $"Position with title {position.Title} already exists in company {position.CompanyId}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error adding position in company {CompanyId}", position.CompanyId);
            throw;
        }
    }

    public async Task<BasePosition> GetPositionByIdAsync(Guid id)
    {
        try
        {
            var filter = Builders<PositionMongoDb>.Filter.And(
                Builders<PositionMongoDb>.Filter.Eq(p => p.Id, id),
                Builders<PositionMongoDb>.Filter.Eq(p => p.IsDeleted, false)
            );
            
            var position = await _positions.Find(filter).FirstOrDefaultAsync();
            
            if (position == null)
            {
                _logger.LogWarning("Position with id {Id} not found", id);
                throw new PositionNotFoundException($"Position with id {id} not found");
            }

            _logger.LogInformation("Position with id {Id} was found", id);
            return PositionConverter.ConvertMongo(position);
        }
        catch (PositionNotFoundException e)
        {
            _logger.LogWarning(e, $"Position with id {id} not found");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error getting position with id - {id}");
            throw;
        }
    }

    public async Task<BasePosition> GetHeadPositionByCompanyIdAsync(Guid id)
    {
        try
        {
            var filter = Builders<PositionMongoDb>.Filter.And(
                Builders<PositionMongoDb>.Filter.Eq(p => p.CompanyId, id),
                Builders<PositionMongoDb>.Filter.Eq(p => p.ParentId, null),
                Builders<PositionMongoDb>.Filter.Eq(p => p.IsDeleted, false)
            );
            
            var position = await _positions.Find(filter).FirstOrDefaultAsync();
            
            if (position == null)
            {
                _logger.LogWarning("Head position for company {CompanyId} not found", id);
                throw new PositionNotFoundException($"Head position for company {id} not found");
            }

            _logger.LogInformation("Head position for company {CompanyId} was found", id);
            return PositionConverter.ConvertMongo(position);
        }
        catch (PositionNotFoundException e)
        {
            _logger.LogWarning(e, $"Head position for company {id} not found");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error getting head position for company - {id}");
            throw;
        }
    }

    public async Task<BasePosition> UpdatePositionTitleAsync(UpdatePosition position)
    {
        try
        {
            var filter = Builders<PositionMongoDb>.Filter.And(
                Builders<PositionMongoDb>.Filter.Eq(p => p.Id, position.Id),
                Builders<PositionMongoDb>.Filter.Eq(p => p.IsDeleted, false)
            );

            var positionToUpdate = await _positions.Find(filter).FirstOrDefaultAsync();
            if (positionToUpdate == null)
                throw new PositionNotFoundException($"Position with id {position.Id} not found");

            // Проверка на дубликат у других позиций
            var duplicateFilter = Builders<PositionMongoDb>.Filter.And(
                Builders<PositionMongoDb>.Filter.Ne(p => p.Id, position.Id),
                Builders<PositionMongoDb>.Filter.Eq(p => p.CompanyId, positionToUpdate.CompanyId),
                Builders<PositionMongoDb>.Filter.Eq(p => p.Title, position.Title),
                Builders<PositionMongoDb>.Filter.Eq(p => p.IsDeleted, false)
            );

            var existingPosition = await _positions.Find(duplicateFilter).FirstOrDefaultAsync();
            if (existingPosition != null)
                throw new PositionAlreadyExistsException(
                    $"Position with title {position.Title} already exists in company {positionToUpdate.CompanyId}");

            var updateDefinition = Builders<PositionMongoDb>.Update
                .Set(p => p.Title, position.Title)
                .Set(p => p.UpdatedAt, DateTime.UtcNow);

            var options = new FindOneAndUpdateOptions<PositionMongoDb> 
            { 
                ReturnDocument = ReturnDocument.After 
            };

            var updatedPosition = await _positions.FindOneAndUpdateAsync(
                filter, updateDefinition, options);

            if (updatedPosition == null)
                throw new PositionNotFoundException($"Position with id {position.Id} not found after update");

            _logger.LogInformation("Position with id {Id} title was updated", position.Id);
            return PositionConverter.ConvertMongo(updatedPosition);
        }
        catch (PositionNotFoundException e)
        {
            _logger.LogWarning(e, $"Position with id {position.Id} not found");
            throw;
        }
        catch (PositionAlreadyExistsException e)
        {
            _logger.LogWarning(e, "Position with same title already exists");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error updating position title with id - {position.Id}");
            throw;
        }
    }

    public async Task<BasePosition> UpdatePositionParentWithSubordinatesAsync(UpdatePosition position)
    {
        // В MongoDB версии нужно будет реализовать логику обновления с подчиненными
        // Это может потребовать дополнительных операций
        _logger.LogInformation("Updating position {Id} parent with subordinates", position.Id);
        return await UpdatePositionTitleAsync(position);
    }

    public async Task<BasePosition> UpdatePositionParentWithoutSuboridnatesAsync(UpdatePosition position)
    {
        // В MongoDB версии нужно будет реализовать логику обновления без подчиненных
        _logger.LogInformation("Updating position {Id} parent without subordinates", position.Id);
        return await UpdatePositionTitleAsync(position);
    }

    public async Task DeletePositionAsync(Guid id)
    {
        try
        {
            var filter = Builders<PositionMongoDb>.Filter.And(
                Builders<PositionMongoDb>.Filter.Eq(p => p.Id, id),
                Builders<PositionMongoDb>.Filter.Eq(p => p.IsDeleted, false)
            );

            var position = await _positions.Find(filter).FirstOrDefaultAsync();
            if (position == null)
                throw new PositionNotFoundException($"Position with id {id} not found");

            // Проверяем, есть ли подчиненные позиции
            var subordinatesFilter = Builders<PositionMongoDb>.Filter.And(
                Builders<PositionMongoDb>.Filter.Eq(p => p.ParentId, id),
                Builders<PositionMongoDb>.Filter.Eq(p => p.IsDeleted, false)
            );

            var subordinatesCount = await _positions.CountDocumentsAsync(subordinatesFilter);
            if (subordinatesCount > 0)
                throw new InvalidOperationException($"Cannot delete position {id} because it has {subordinatesCount} subordinate positions");

            // Помечаем позицию как удаленную
            var updateDefinition = Builders<PositionMongoDb>.Update
                .Set(p => p.IsDeleted, true)
                .Set(p => p.UpdatedAt, DateTime.UtcNow);

            var result = await _positions.UpdateOneAsync(filter, updateDefinition);

            if (result.ModifiedCount == 0)
                throw new PositionNotFoundException($"Position with id {id} not found for deletion");

            _logger.LogInformation("Position with id {Id} was deleted", id);
        }
        catch (PositionNotFoundException e)
        {
            _logger.LogWarning(e, $"Position with id {id} not found for deletion");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error deleting position with id - {id}");
            throw;
        }
    }

    public async Task<IEnumerable<PositionHierarchy>> GetSubordinatesAsync(Guid parentId)
    {
        try
        {
            List<PositionHierarchy> result = new List<PositionHierarchy>();
            var head = await _positions.Find(p => p.ParentId == parentId).FirstOrDefaultAsync();
            if (head is null)
                throw new PositionNotFoundException($"Position with id {parentId} not found");
            result.Add(new PositionHierarchy(head.Id, head.ParentId, head.Title, 0));
            int i = 0;
            while (i != result.Count)
            {
                var children = await _positions.Find(p => p.ParentId == result[i].PositionId).ToListAsync();
                result.AddRange(children.Select(e => new PositionHierarchy(e.Id, e.ParentId, e.Title, result[i].Level+1)));
                ++i;
            }
            _logger.LogInformation("Getting subordinates for position {ParentId}", parentId);
            
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error getting subordinates for position - {parentId}");
            throw;
        }
    }
}
