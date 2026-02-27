using Database.Context;
using Database.Models;
using Database.Models.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Project.Core.Exceptions;
using Project.Core.Models;
using Project.Core.Models.Position;
using Project.Core.Models.PositionHistory;
using Project.Core.Repositories;

namespace Database.Repositories;

public class PositionRepository : IPositionRepository
{
    private readonly ProjectDbContext _context;
    private readonly ILogger<PositionRepository> _logger;

    public PositionRepository(ProjectDbContext context, ILogger<PositionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BasePosition> AddPositionAsync(CreatePosition position)
    {
        try
        {
            var positionDb = PositionConverter.Convert(position);
            if (positionDb is null)
            {
                _logger.LogWarning("Failed to convert CreatePosition to PositionDb");
                throw new ArgumentException("Failed to convert CreatePosition to PositionDb");
            }

            var existingPosition = await _context.PositionDb
                .FirstOrDefaultAsync(p => p.CompanyId == position.CompanyId && p.Title == position.Title);

            if (existingPosition is not null)
            {
                _logger.LogWarning("Position with title {Title} already exists in company {CompanyId}", position.Title,
                    position.CompanyId);
                throw new PositionAlreadyExistsException(
                    $"Position with title {position.Title} already exists in company {position.CompanyId}");
            }

            await _context.PositionDb.AddAsync(positionDb);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Position with id {Id} was added", positionDb.Id);
            return PositionConverter.Convert(positionDb)!;
        }
        catch (Exception e) when (e is not PositionAlreadyExistsException)
        {
            _logger.LogError(e, "Error occurred while adding position");
            throw;
        }
    }

    public async Task<BasePosition> GetPositionByIdAsync(Guid id)
    {
        try
        {
            var position = await _context.PositionDb
                .FirstOrDefaultAsync(p => p.Id == id);

            if (position is null)
            {
                _logger.LogWarning("Position with id {Id} not found", id);
                throw new PositionNotFoundException($"Position with id {id} not found");
            }

            _logger.LogInformation("Position with id {Id} was retrieved", id);
            return PositionConverter.Convert(position)!;
        }
        catch (Exception e) when (e is not PositionNotFoundException)
        {
            _logger.LogError(e, "Error occurred while getting position with id {Id}", id);
            throw;
        }
    }

    public async Task<BasePosition> GetHeadPositionByCompanyIdAsync(Guid id)
    {
        try
        {
            var positionDb = await  _context.PositionDb.FirstOrDefaultAsync(p => p.CompanyId==id && p.ParentId == null);
            if (positionDb is null)
            {
                _logger.LogWarning("Head position for company with id {Id} not found", id);
                throw new PositionNotFoundException($"Head position for company with id {id} not found");
            }

            return PositionConverter.Convert(positionDb);
        }
        catch (Exception e)
        {
            _logger.LogWarning($"Error getting head position for company with id {id}");
            throw;
        }
    }

    public async Task<BasePosition> UpdatePositionTitleAsync(Guid id, string? title)
    {
        try
        {
            var positionDb = await _context.PositionDb
                .FirstOrDefaultAsync(p => p.Id == id);

            if (positionDb is null)
            {
                _logger.LogWarning("Position with id {Id} not found for update", id);
                throw new PositionNotFoundException($"Position with id {id} not found");
            }

            var existingPosition = await _context.PositionDb
                .Where(p => p.Id != id &&
                            p.CompanyId == positionDb.CompanyId &&
                            p.Title == positionDb.Title)
                .FirstOrDefaultAsync();

            if (existingPosition is not null)
            {
                _logger.LogWarning("Position with title {Title} already exists in company {CompanyId}", positionDb.Title,
                    positionDb.CompanyId);
                throw new PositionAlreadyExistsException(
                    $"Position with title {positionDb.Title} already exists in company {positionDb.CompanyId}");
            }

            positionDb.Title = title ?? positionDb.Title;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Position with id {Id} was updated", positionDb.Id);
            return PositionConverter.Convert(positionDb)!;
        }
        catch (Exception e) when (e is not PositionNotFoundException and not PositionAlreadyExistsException)
        {
            _logger.LogError(e, "Error occurred while updating position with id {Id}", id);
            throw;
        }
    }

    public async Task<BasePosition> UpdatePositionParentWithSubordinatesAsync(Guid id, Guid? parentId)
    {
        try
        {
            
            var positionDb = await _context.PositionDb
                .FirstOrDefaultAsync(p => p.Id == id);

            if (positionDb is null)
            {
                _logger.LogWarning("Position with id {Id} not found for update", id);
                throw new PositionNotFoundException($"Position with id {id} not found");
            }

            var existingPosition = await _context.PositionDb
                .Where(p => p.Id != positionDb.Id &&
                            p.CompanyId == positionDb.CompanyId &&
                            p.Title == positionDb.Title)
                .FirstOrDefaultAsync();

            if (existingPosition is not null)
            {
                _logger.LogWarning("Position with title {Title} already exists in company {CompanyId}", positionDb.Title,
                    positionDb.CompanyId);
                throw new PositionAlreadyExistsException(
                    $"Position with title {positionDb.Title} already exists in company {positionDb.CompanyId}");
            }

            positionDb.ParentId = parentId;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Position with id {Id} was updated", id);
            return PositionConverter.Convert(positionDb)!;
        }
        catch (Exception e) when (e is not PositionNotFoundException and not PositionAlreadyExistsException)
        {
            _logger.LogError(e, "Error occurred while updating position with id {Id}", id);
            throw;
        }
    }

    public async Task<BasePosition> UpdatePositionParentWithoutSuboridnatesAsync(Guid id, Guid? parentId)
    {
        try
        {
            var positionDb = await _context.PositionDb
                .FirstOrDefaultAsync(p => p.Id == id);

            if (positionDb is null)
            {
                _logger.LogWarning("Position with id {Id} not found for update", id);
                throw new PositionNotFoundException($"Position with id {id} not found");
            }

            var existingPosition = await _context.PositionDb
                .Where(p => p.Id != id &&
                            p.CompanyId == positionDb.CompanyId &&
                            p.Title == positionDb.Title)
                .FirstOrDefaultAsync();

            if (existingPosition is not null)
            {
                _logger.LogWarning("Position with title {Title} already exists in company {CompanyId}", positionDb.Title,
                    positionDb.CompanyId);
                throw new PositionAlreadyExistsException(
                    $"Position with title {positionDb.Title} already exists in company {positionDb.CompanyId}");
            }
            
            var children = _context.PositionDb.Where(e=>e.ParentId == id).ToList();

            children.ForEach(e => e.ParentId = positionDb.ParentId);

            positionDb.ParentId = parentId;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Position with id {Id} was updated", id);
            return PositionConverter.Convert(positionDb)!;
        }
        catch (Exception e) when (e is not PositionNotFoundException and not PositionAlreadyExistsException)
        {
            _logger.LogError(e, "Error occurred while updating position with id {Id}", id);
            throw;
        }
    }

    public async Task DeletePositionAsync(Guid id)
    {
        try
        {
            var position = await _context.PositionDb
                .FirstOrDefaultAsync(p => p.Id == id);

            if (position is null)
            {
                _logger.LogWarning("Position with id {Id} not found for deletion", id);
                throw new PositionNotFoundException($"Position with id {id} not found");
            }

            _context.PositionDb.Remove(position);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Position with id {Id} was deleted", id);
        }
        catch (Exception e) when (e is not PositionNotFoundException)
        {
            _logger.LogError(e, "Error occurred while deleting position with id {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<PositionHierarchy>> GetSubordinatesAsync(Guid parentId)
    {
        try
        {
            
            List<PositionHierarchy> result = new List<PositionHierarchy>();
            var head = await _context.PositionDb.Where(e => e.ParentId == parentId).FirstOrDefaultAsync();
            if (head is null)
                throw new PositionNotFoundException($"Position with id {parentId} not found");
            result.Add(new PositionHierarchy(head.Id, head.ParentId, head.Title, 0));
            int i = 0;
            while (i != result.Count)
            {
                var children = await _context.PositionDb.Where(e => e.ParentId == result[i].PositionId).ToListAsync();
                result.AddRange(children.Select(e => new PositionHierarchy(e.Id, e.ParentId, e.Title, result[i].Level+1)));
                ++i;
            }
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while getting subordinates for position {ParentId}", parentId);
            throw;
        }
    }
}