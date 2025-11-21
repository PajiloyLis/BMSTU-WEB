using System.Text.Json;
using Microsoft.Extensions.Logging;
using Project.Core.Models;
using Project.Core.Models.Post;
using Project.Core.Repositories;
using Project.Core.Services;
using StackExchange.Redis;

namespace Project.Services.PostService;

public class PostService : IPostService
{
    private readonly ILogger<PostService> _logger;
    private readonly IPostRepository _postRepository;

    public PostService(IPostRepository postRepository, ILogger<PostService> logger,
        IConnectionMultiplexer connectionMultiplexer)
    {
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BasePost> AddPostAsync(string title, decimal salary, Guid companyId)
    {
        try
        {
            var post = new CreatePost(title, salary, companyId);
            var result = await _postRepository.AddPostAsync(post);
            _logger.LogInformation("Post with id {Id} was added", result.Id);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while adding post");
            throw;
        }
    }

    public async Task<BasePost> GetPostByIdAsync(Guid postId)
    {
        try
        {
            var result = await _postRepository.GetPostByIdAsync(postId);

           _logger.LogInformation("Post with id {Id} was retrieved", postId);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while getting post with id {Id}", postId);
            throw;
        }
    }

    public async Task<BasePost> UpdatePostAsync(Guid postId, string? title = null,
        decimal? salary = null)
    {
        try
        {
            var post = new UpdatePost(postId, title, salary);
            var result = await _postRepository.UpdatePostAsync(post);
            _logger.LogInformation("Post with id {Id} was updated", postId);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while updating post with id {Id}", postId);
            throw;
        }
    }

    public async Task<IEnumerable<BasePost>> GetPostsByCompanyIdAsync(Guid companyId, int pageNumber, int pageSize)
    {
        try
        {
            var result = await _postRepository.GetPostsAsync(companyId, pageNumber, pageSize);

            _logger.LogInformation("Posts for company {CompanyId} were retrieved", companyId);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while getting posts for company {CompanyId}", companyId);
            throw;
        }
    }

    public async Task DeletePostAsync(Guid postId)
    {
        try
        {
            await _postRepository.DeletePostAsync(postId);
            _logger.LogInformation("Post with id {Id} was deleted", postId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while deleting post with id {Id}", postId);
            throw;
        }
    }
}