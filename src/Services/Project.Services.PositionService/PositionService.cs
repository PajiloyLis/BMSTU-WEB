using System.Text.Json;
using Microsoft.Extensions.Logging;
using Project.Core.Models;
using Project.Core.Models.Position;
using Project.Core.Models.PositionHistory;
using Project.Core.Repositories;
using Project.Core.Services;

namespace Project.Services.PositionService;

public class PositionService : IPositionService
{
    private readonly ILogger<PositionService> _logger;
    private readonly IPositionRepository _repository;

    public PositionService(IPositionRepository repository, ILogger<PositionService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BasePosition> AddPositionAsync(Guid? parentId, string title, Guid companyId)
    {
        try
        {
            var model = new CreatePosition(parentId, title, companyId);
            var result = await _repository.AddPositionAsync(model);
            _logger.LogInformation("Position added: {Id}", result.Id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding position");
            throw;
        }
    }

    public async Task<BasePosition> GetPositionByIdAsync(Guid id)
    {
        try
        {
            var result = await _repository.GetPositionByIdAsync(id);
            _logger.LogInformation("Position retrieved: {Id}", id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting position by id {Id}", id);
            throw;
        }
    }

    public async Task<BasePosition> GetHeadPositionByCompanyIdAsync(Guid id)
    {
        try
        {
            var result = await _repository.GetHeadPositionByCompanyIdAsync(id);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogWarning($"Error getting head position for company with id {id}");
            throw;
        }
    }

    public async Task<BasePosition> UpdatePositionTitleAsync(Guid id,
        string? title = null)
    {
        try
        {
            var result = await _repository.UpdatePositionTitleAsync(id, title);
            _logger.LogInformation("Position updated: {Id}", id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating position {Id}", id);
            throw;
        }
    }

    // public async Task<BasePosition> UpdatePositionParentWithSubordinatesAsync(Guid id, Guid companyId, Guid? parentId = null, string? title = null)
    // {
    //     try
    //     {
    //         if (parentId is null)
    //         {
    //             _logger.LogWarning("Parent id must be not null");
    //             throw new ArgumentNullException(nameof(parentId), "Parent id must be not null");
    //         }
    //         var model = new UpdatePosition(id, companyId, parentId, title);
    //         var result = await _repository.UpdatePositionParentWithSubordinatesAsync(model, TODO);
    //         _logger.LogInformation("Position updated: {Id}", id);
    //         return result;
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error updating position {Id}", id);
    //         throw;
    //     }
    // }

    public async Task<BasePosition> UpdatePositionParent(Guid id, Guid? parentId, PositionUpdateMode updateMode)
    {
        try
        {
            if (parentId is null)
            {
                _logger.LogWarning("Parent id must be not null");
                throw new ArgumentNullException(nameof(parentId), "Parent id must be not null");
            }
            
            BasePosition result;
            
            switch (updateMode)
            {
                case PositionUpdateMode.UpdateWithoutSubordinates:
                    result = await _repository.UpdatePositionParentWithoutSuboridnatesAsync(id, parentId);
                    break;
                default:
                    result = await _repository.UpdatePositionParentWithSubordinatesAsync(id, parentId);
                    break;
            }
            _logger.LogInformation("Position updated: {Id}", id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating position {Id}", id);
            throw;
        }
    }

    public async Task DeletePositionAsync(Guid id)
    {
        try
        {
            await _repository.DeletePositionAsync(id);
            _logger.LogInformation("Position deleted: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting position {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<PositionHierarchy>> GetSubordinatesAsync(Guid parentId)
    {
        try
        {
            var result = await _repository.GetSubordinatesAsync(parentId);
            _logger.LogInformation("Subordinates retrieved for parentId: {ParentId}", parentId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subordinates for parentId {ParentId}", parentId);
            throw;
        }
    }
}