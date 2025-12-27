using Project.Core.Models.PositionHistory;

namespace Project.Core.Services;

public interface IPositionHistoryService
{
    /// <summary>
    /// Creates a new position history record
    /// </summary>
    /// <param name="positionId">Position ID</param>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="startDate">Start date of the position</param>
    /// <param name="endDate">End date of the position (optional)</param>
    /// <returns>Created position history record</returns>
    Task<BasePositionHistory> AddPositionHistoryAsync(
        Guid positionId,
        Guid employeeId,
        DateOnly startDate,
        DateOnly? endDate = null);

    /// <summary>
    /// Gets a position history record by its ID
    /// </summary>
    /// <param name="positionId">Position ID</param>
    /// <param name="employeeId">Employee ID</param>
    /// <returns>Position history record</returns>
    /// <exception cref="PositionHistoryNotFoundException">Thrown when position history record is not found</exception>
    Task<BasePositionHistory> GetPositionHistoryAsync(Guid positionId, Guid employeeId);

    /// <summary>
    /// Updates an existing position history record
    /// </summary>
    /// <param name="positionId">Position ID</param>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="startDate">New start date (optional)</param>
    /// <param name="endDate">New end date (optional)</param>
    /// <returns>Updated position history record</returns>
    /// <exception cref="PositionHistoryNotFoundException">Thrown when position history record is not found</exception>
    Task<BasePositionHistory> UpdatePositionHistoryAsync(
        Guid positionId,
        Guid employeeId,
        DateOnly? startDate = null,
        DateOnly? endDate = null);

    /// <summary>
    /// Deletes a position history record
    /// </summary>
    /// <param name="positionId">Position ID</param>
    /// <param name="employeeId">Employee ID</param>
    /// <exception cref="PositionHistoryNotFoundException">Thrown when position history record is not found</exception>
    Task DeletePositionHistoryAsync(Guid positionId, Guid employeeId);

    /// <summary>
    /// Gets paginated position history records for a specific employee within a date range
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="startDate">Start date of the range</param>
    /// <param name="endDate">End date of the range</param>
    /// <returns>Paginated list of position history records</returns>
    Task<IEnumerable<BasePositionHistory>> GetPositionHistoryByEmployeeIdAsync(Guid employeeId,
        DateOnly? startDate,
        DateOnly? endDate);

    /// <summary>
    /// Gets paginated position history records for current subordinates of a specific manager
    /// </summary>
    /// <param name="managerId">Manager's employee ID</param>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <returns>Paginated list of current position history records for subordinates</returns>
    Task<IEnumerable<PositionHierarchyWithEmployee>> GetCurrentSubordinatesAsync(Guid managerId, int pageNumber,
        int pageSize);
    
    Task<IEnumerable<BasePositionHistory>> GetCurrentSubordinatesPositionHistoryAsync(Guid managerId,
        DateOnly? startDate, DateOnly? endDate, int pageNumber, int pageSize);

    Task<IEnumerable<BasePositionHistory>> GetCurrentSubordinatesByPositionAsync(Guid managerPositionId, DateOnly? startDate, DateOnly? endDate);
}