using Project.Core.Models.PostHistory;

namespace Project.Core.Repositories;

public interface IPostHistoryRepository
{
    /// <summary>
    /// Creates a new post history record
    /// </summary>
    /// <param name="postId">Post ID</param>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="startDate">Start date of the post</param>
    /// <param name="endDate">End date of the post (optional)</param>
    /// <returns>Created post history record</returns>
    Task<BasePostHistory> AddPostHistoryAsync(CreatePostHistory createPostHistory);

    /// <summary>
    /// Gets a post history record by its ID
    /// </summary>
    /// <param name="postId">Post ID</param>
    /// <param name="employeeId">Employee ID</param>
    /// <returns>Post history record</returns>
    /// <exception cref="PostHistoryNotFoundException">Thrown when post history record is not found</exception>
    Task<BasePostHistory> GetPostHistoryByIdAsync(Guid postId, Guid employeeId);

    /// <summary>
    /// Updates an existing post history record
    /// </summary>
    /// <param name="postId">Post ID</param>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="startDate">New start date (optional)</param>
    /// <param name="endDate">New end date (optional)</param>
    /// <returns>Updated post history record</returns>
    /// <exception cref="PostHistoryNotFoundException">Thrown when post history record is not found</exception>
    Task<BasePostHistory> UpdatePostHistoryAsync(UpdatePostHistory updatePostHistory);

    /// <summary>
    /// Deletes a post history record
    /// </summary>
    /// <param name="postId">Post ID</param>
    /// <param name="employeeId">Employee ID</param>
    /// <exception cref="PostHistoryNotFoundException">Thrown when post history record is not found</exception>
    Task DeletePostHistoryAsync(Guid postId, Guid employeeId);

    /// <summary>
    /// Gets paginated post history records for a specific employee within a date range
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="startDate">Start date of the range</param>
    /// <param name="endDate">End date of the range</param>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <returns>Paginated list of post history records</returns>
    Task<IEnumerable<BasePostHistory>> GetPostHistoryByEmployeeIdAsync(Guid employeeId,
        DateOnly? startDate,
        DateOnly? endDate, int pageNumber, int pageSize);

    /// <summary>
    /// Gets paginated post history records for subordinates of a specific manager within a date range
    /// </summary>
    /// <param name="managerId">Manager's employee ID</param>
    /// <param name="startDate">Start date of the range</param>
    /// <param name="endDate">End date of the range</param>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <returns>Paginated list of post history records for subordinates</returns>
    Task<IEnumerable<BasePostHistory>> GetSubordinatesPostHistoryAsync(Guid managerId,
        DateOnly? startDate,
        DateOnly? endDate, int pageNumber, int pageSize);
}