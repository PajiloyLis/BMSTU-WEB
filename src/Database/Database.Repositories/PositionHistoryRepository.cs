using Database.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Project.Core.Exceptions;
using Project.Core.Models.PositionHistory;
using Project.Core.Repositories;
using Project.Database.Models.Converters;

namespace Project.Database.Repositories;

public class PositionHistoryRepository : IPositionHistoryRepository
{
    private readonly ProjectDbContext _context;
    private readonly ILogger<PositionHistoryRepository> _logger;

    public PositionHistoryRepository(
        ProjectDbContext context,
        ILogger<PositionHistoryRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BasePositionHistory> AddPositionHistoryAsync(CreatePositionHistory createPositionHistory)
    {
        try
        {
            _logger.LogInformation(
                "Adding position history for employee {EmployeeId} and position {PositionId}",
                createPositionHistory.EmployeeId, createPositionHistory.PositionId);

            var positionHistoryDb = PositionHistoryConverter.Convert(createPositionHistory);
            if (positionHistoryDb == null)
                throw new ArgumentNullException(nameof(createPositionHistory));
            var previousOpenPosition = await _context.PositionHistoryDb.Where(e =>
                e.EmployeeId == createPositionHistory.EmployeeId && e.PositionId == createPositionHistory.PositionId &&
                e.EndDate == null).ToListAsync();
            previousOpenPosition.ForEach(e => e.EndDate = DateOnly.FromDateTime(DateTime.Today));
            await _context.PositionHistoryDb.AddAsync(positionHistoryDb);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully added position history for employee {EmployeeId} and position {PositionId}",
                createPositionHistory.EmployeeId, createPositionHistory.PositionId);

            return PositionHistoryConverter.Convert(positionHistoryDb)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error adding position history for employee {EmployeeId} and position {PositionId}",
                createPositionHistory.EmployeeId, createPositionHistory.PositionId);
            throw;
        }
    }

    public async Task<BasePositionHistory> GetPositionHistoryByIdAsync(Guid positionId, Guid employeeId)
    {
        try
        {
            _logger.LogInformation(
                "Getting position history for employee {EmployeeId} and position {PositionId}",
                employeeId, positionId);

            var positionHistoryDb = await _context.PositionHistoryDb
                .FirstOrDefaultAsync(x => x.PositionId == positionId && x.EmployeeId == employeeId);

            if (positionHistoryDb == null)
            {
                _logger.LogWarning(
                    "Position history not found for employee {EmployeeId} and position {PositionId}",
                    employeeId, positionId);
                throw new PositionHistoryNotFoundException();
            }

            _logger.LogInformation(
                "Successfully retrieved position history for employee {EmployeeId} and position {PositionId}",
                employeeId, positionId);

            return PositionHistoryConverter.Convert(positionHistoryDb)!;
        }
        catch (PositionHistoryNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting position history for employee {EmployeeId} and position {PositionId}",
                employeeId, positionId);
            throw;
        }
    }

    public async Task<BasePositionHistory> UpdatePositionHistoryAsync(UpdatePositionHistory updatePositionHistory)
    {
        try
        {
            _logger.LogInformation(
                "Updating position history for employee {EmployeeId} and position {PositionId}",
                updatePositionHistory.EmployeeId, updatePositionHistory.PositionId);

            var positionHistoryDb = await _context.PositionHistoryDb
                .FirstOrDefaultAsync(x => x.PositionId == updatePositionHistory.PositionId &&
                                          x.EmployeeId == updatePositionHistory.EmployeeId);

            if (positionHistoryDb == null)
            {
                _logger.LogWarning(
                    "Position history not found for employee {EmployeeId} and position {PositionId}",
                    updatePositionHistory.EmployeeId, updatePositionHistory.PositionId);
                throw new PositionHistoryNotFoundException();
            }

            if (updatePositionHistory.StartDate.HasValue)
                positionHistoryDb.StartDate = updatePositionHistory.StartDate.Value;

            if (updatePositionHistory.EndDate.HasValue)
                positionHistoryDb.EndDate = updatePositionHistory.EndDate.Value;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully updated position history for employee {EmployeeId} and position {PositionId}",
                updatePositionHistory.EmployeeId, updatePositionHistory.PositionId);

            return PositionHistoryConverter.Convert(positionHistoryDb)!;
        }
        catch (PositionHistoryNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating position history for employee {EmployeeId} and position {PositionId}",
                updatePositionHistory.EmployeeId, updatePositionHistory.PositionId);
            throw;
        }
    }

    public async Task DeletePositionHistoryAsync(Guid positionId, Guid employeeId)
    {
        try
        {
            _logger.LogInformation(
                "Deleting position history for employee {EmployeeId} and position {PositionId}",
                employeeId, positionId);

            var positionHistoryDb = await _context.PositionHistoryDb
                .FirstOrDefaultAsync(x => x.PositionId == positionId && x.EmployeeId == employeeId);

            if (positionHistoryDb == null)
            {
                _logger.LogWarning(
                    "Position history not found for employee {EmployeeId} and position {PositionId}",
                    employeeId, positionId);
                throw new PositionHistoryNotFoundException();
            }

            _context.PositionHistoryDb.Remove(positionHistoryDb);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully deleted position history for employee {EmployeeId} and position {PositionId}",
                employeeId, positionId);
        }
        catch (PositionHistoryNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error deleting position history for employee {EmployeeId} and position {PositionId}",
                employeeId, positionId);
            throw;
        }
    }

    public async Task<IEnumerable<BasePositionHistory>> GetPositionHistoryByEmployeeIdAsync(Guid employeeId,
        DateOnly? startDate,
        DateOnly? endDate)
    {
        try
        {
            var query = _context.PositionHistoryDb
                .Where(x => x.EmployeeId == employeeId);

            if (startDate.HasValue)
                query = query.Where(s => s.EndDate == null || s.EndDate >= startDate);
            if (endDate.HasValue)
                query = query.Where(s =>
                    (s.EndDate == null && endDate == DateOnly.FromDateTime(DateTime.Today)) || s.EndDate <= endDate);

            var items = await query
                .OrderByDescending(x => x.StartDate)
                .Select(x => PositionHistoryConverter.Convert(x)!)
                .ToListAsync();

            _logger.LogInformation(
                "Successfully retrieved {Count} position history records for employee {EmployeeId}",
                items.Count, employeeId);

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting position history for employee {EmployeeId}",
                employeeId);
            throw;
        }
    }

    public async Task<IEnumerable<PositionHierarchyWithEmployee>> GetCurrentSubordinatesAsync(Guid managerId,
        int pageNumber, int pageSize)
    {
        try
        {
            _logger.LogInformation(
                "Getting current subordinates position history for manager {ManagerId}",
                managerId);

            var positionHistories = await GetAllCurrentSubordinates(managerId);

            positionHistories = positionHistories.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            _logger.LogInformation(
                "Successfully retrieved {Count} current subordinates position history records for manager {ManagerId}",
                positionHistories.Count, managerId);

            return positionHistories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting current subordinates position history for manager {ManagerId}",
                managerId);
            throw;
        }
    }

    public async Task<IEnumerable<BasePositionHistory>> GetCurrentSubordinatesPositionHistoryAsync(Guid managerId,
        DateOnly? startDate, DateOnly? endDate, int pageNumber, int pageSize)
    {
        try
        {
            _logger.LogInformation(
                "Getting current subordinates position history for manager {ManagerId}", managerId);

            var allSubordinates = await GetAllCurrentSubordinates(managerId);
            var employees = allSubordinates.Select(ph => ph.EmployeeId).ToList();

            var query = _context.PositionHistoryDb.Where(ph => employees.Contains(ph.EmployeeId));

            if (startDate.HasValue)
                query = query.Where(ph => ph.EndDate == null || ph.EndDate >= startDate);
            if (endDate.HasValue)
                query = query.Where(ph =>
                    (ph.EndDate == null && endDate == DateOnly.FromDateTime(DateTime.Today)) || ph.EndDate <= endDate);

            var items = await query
                .Select(x => PositionHistoryConverter.Convert(x)!)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogInformation(
                "Successfully retrieved {Count} current subordinates position history records for manager {ManagerId}",
                items.Count, managerId);

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting current subordinates position history for manager {ManagerId}",
                managerId);
            throw;
        }
    }

    public async Task<BasePositionHistory> GetCurrentEmployeePositionByEmployeeIdAsync(Guid employeeId)
    {
        try
        {
            _logger.LogInformation($"Getting current position for employee {employeeId}");

            var cur_pos =
                await _context.PositionHistoryDb.FirstOrDefaultAsync(ph =>
                    ph.EmployeeId == employeeId && ph.EndDate == null);
            if (cur_pos is null)
                throw new PositionHistoryNotFoundException($"Position history not found for employee {employeeId}");

            return PositionHistoryConverter.Convert(cur_pos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting current position for employee {employeeId}");
            throw;
        }
    }

    private async Task<List<PositionHierarchyWithEmployee>> GetAllCurrentSubordinates(Guid managerId)
    {
        try
        {
            var subordinates = new List<PositionHierarchyWithEmployee>();
            var head = await _context.PositionHistoryDb.Where(e => e.EmployeeId == managerId && e.EndDate == null)
                .FirstOrDefaultAsync();
            if (head is null)
                throw new PositionHistoryNotFoundException($"Current position for employee {managerId} not found");
            var headPosition = await _context.PositionDb.Where(e => e.Id == head.PositionId).FirstOrDefaultAsync();
            subordinates.Add(new PositionHierarchyWithEmployee(head.EmployeeId, head.PositionId, headPosition.ParentId,
                headPosition.Title, 0));
            var i = 0;
            while (i != subordinates.Count)
            {
                var subordinatesPositions =
                    await _context.PositionDb.Where(e => e.ParentId == subordinates[i].PositionId).ToListAsync();
                var children = await _context.PositionHistoryDb.Where(e =>
                        subordinatesPositions.Select(e => e.Id).ToList().Contains(e.PositionId) && e.EndDate == null)
                    .ToListAsync();
                var resultChildren = children.Select(e =>
                {
                    var position = subordinatesPositions.Where(p => p.Id == e.PositionId).FirstOrDefault();
                    return new PositionHierarchyWithEmployee(e.EmployeeId, e.PositionId, position.ParentId,
                        position.Title,
                        subordinates[i].Level + 1);
                });
                subordinates.AddRange(resultChildren);
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