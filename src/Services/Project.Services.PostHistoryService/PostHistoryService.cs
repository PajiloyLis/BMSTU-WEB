using System.Text.Json;
using Microsoft.Extensions.Logging;
using Project.Core.Exceptions;
using Project.Core.Models.PostHistory;
using Project.Core.Repositories;
using Project.Core.Services;
using StackExchange.Redis;

namespace Project.Services.PostHistoryService;

public class PostHistoryService : IPostHistoryService
{
    private readonly ILogger<PostHistoryService> _logger;
    private readonly IPostHistoryRepository _repository;

    public PostHistoryService(
        IPostHistoryRepository repository,
        ILogger<PostHistoryService> logger, IConnectionMultiplexer cache)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BasePostHistory> AddPostHistoryAsync(
        Guid postId,
        Guid employeeId,
        DateOnly startDate,
        DateOnly? endDate = null)
    {
        try
        {
            var createPostHistory = new CreatePostHistory(postId, employeeId, startDate, endDate);
            var postHistory = await _repository.AddPostHistoryAsync(createPostHistory);
            return postHistory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding post history for employee {EmployeeId} and post {PostId}",
                employeeId, postId);
            throw;
        }
    }

    public async Task<BasePostHistory> GetPostHistoryAsync(Guid postId, Guid employeeId)
    {
        try
        {
            var postHistory = await _repository.GetPostHistoryByIdAsync(postId, employeeId);
            return postHistory;
        }
        catch (PostHistoryNotFoundException)
        {
            _logger.LogWarning("Post history not found for employee {EmployeeId} and post {PostId}",
                employeeId, postId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting post history for employee {EmployeeId} and post {PostId}",
                employeeId, postId);
            throw;
        }
    }

    public async Task<BasePostHistory> UpdatePostHistoryAsync(
        Guid postId,
        Guid employeeId,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        try
        {
            var updatePostHistory = new UpdatePostHistory(postId, employeeId, startDate, endDate);
            var postHistory = await _repository.UpdatePostHistoryAsync(updatePostHistory);
            return postHistory;
        }
        catch (PostHistoryNotFoundException)
        {
            _logger.LogWarning("Post history not found for employee {EmployeeId} and post {PostId}",
                employeeId, postId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating post history for employee {EmployeeId} and post {PostId}",
                employeeId, postId);
            throw;
        }
    }

    public async Task DeletePostHistoryAsync(Guid postId, Guid employeeId)
    {
        try
        {
            await _repository.DeletePostHistoryAsync(postId, employeeId);
        }
        catch (PostHistoryNotFoundException)
        {
            _logger.LogWarning("Post history not found for employee {EmployeeId} and post {PostId}",
                employeeId, postId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post history for employee {EmployeeId} and post {PostId}",
                employeeId, postId);
            throw;
        }
    }

    public async Task<IEnumerable<BasePostHistory>> GetPostHistoryByEmployeeIdAsync(Guid employeeId,
        DateOnly? startDate,
        DateOnly? endDate, int pageNumber, int pageSize)
    {
        try
        {
            return await _repository.GetPostHistoryByEmployeeIdAsync(
                employeeId,
                startDate,
                endDate, pageNumber, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting post history for employee {EmployeeId}", employeeId);
            throw;
        }
    }

    public async Task<IEnumerable<BasePostHistory>> GetSubordinatesPostHistoryAsync(Guid managerId,
        DateOnly? startDate,
        DateOnly? endDate, int pageNumber, int pageSize)
    {
        try
        {
            return await _repository.GetSubordinatesPostHistoryAsync(
                managerId,
                startDate,
                endDate, pageNumber, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subordinates post history for manager {ManagerId}", managerId);
            throw;
        }
    }
}