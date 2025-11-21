// using Database.Context;
// using Database.Models;
// using Database.Models.Converters;
// using Microsoft.Extensions.Logging;
// using MongoDB.Driver;
// using Project.Core.Exceptions;
// using Project.Core.Models;
// using Project.Core.Models.Post;
// using Project.Core.Repositories;
//
// namespace Database.Repositories;
//
// public class PostRepositoryMongo : IPostRepository
// {
//     private readonly IMongoCollection<PostMongoDb> _posts;
//     private readonly ILogger<PostRepository> _logger;
//
//     public PostRepositoryMongo(MongoDbContext context, ILogger<PostRepository> logger)
//     {
//         _posts = context.Posts;
//         _logger = logger;
//         
//         // Создаем индексы для производительности
//         // CreateIndexes();
//     }
//
//     private void CreateIndexes()
//     {
//         // Составной индекс для проверки дубликатов
//         _posts.Indexes.CreateOne(
//             new CreateIndexModel<PostMongoDb>(
//                 Builders<PostMongoDb>.IndexKeys
//                     .Ascending(p => p.CompanyId)
//                     .Ascending(p => p.Title)
//                     .Ascending(p => p.IsDeleted),
//                 new CreateIndexOptions { Unique = true }
//             ));
//
//         // Индекс для поиска по CompanyId
//         _posts.Indexes.CreateOne(
//             new CreateIndexModel<PostMongoDb>(
//                 Builders<PostMongoDb>.IndexKeys.Ascending(p => p.CompanyId)
//             ));
//
//         // Индекс для сортировки по зарплате
//         _posts.Indexes.CreateOne(
//             new CreateIndexModel<PostMongoDb>(
//                 Builders<PostMongoDb>.IndexKeys.Ascending(p => p.Salary)
//             ));
//     }
//
//     public async Task<BasePost> AddPostAsync(CreatePost post)
//     {
//         var postDb = PostConverter.ConvertMongo(post);
//         postDb.CreatedAt = DateTime.UtcNow;
//         postDb.UpdatedAt = DateTime.UtcNow;
//
//         try
//         {
//             // Проверка на существующий пост
//             var duplicateFilter = Builders<PostMongoDb>.Filter.And(
//                 Builders<PostMongoDb>.Filter.Eq(p => p.CompanyId, postDb.CompanyId),
//                 Builders<PostMongoDb>.Filter.Eq(p => p.Title, postDb.Title),
//                 Builders<PostMongoDb>.Filter.Eq(p => p.IsDeleted, false)
//             );
//
//             var existingPost = await _posts.Find(duplicateFilter).FirstOrDefaultAsync();
//             if (existingPost != null)
//             {
//                 _logger.LogWarning("Post already exists in company {CompanyId}", post.CompanyId);
//                 throw new PostAlreadyExistsException(
//                     $"Post with title {post.Title} already exists in company {post.CompanyId}");
//             }
//
//             await _posts.InsertOneAsync(postDb);
//
//             _logger.LogInformation("Post with id {Id} was added", postDb.Id);
//             return PostConverter.ConvertMongo(postDb);
//         }
//         catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
//         {
//             _logger.LogWarning(ex, "Post already exists in company {CompanyId}", post.CompanyId);
//             throw new PostAlreadyExistsException(
//                 $"Post with title {post.Title} already exists in company {post.CompanyId}");
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, "Error adding post in company {CompanyId}", post.CompanyId);
//             throw;
//         }
//     }
//
//     public async Task<BasePost> GetPostByIdAsync(Guid postId)
//     {
//         try
//         {
//             var filter = Builders<PostMongoDb>.Filter.And(
//                 Builders<PostMongoDb>.Filter.Eq(p => p.Id, postId),
//                 Builders<PostMongoDb>.Filter.Eq(p => p.IsDeleted, false)
//             );
//             
//             var post = await _posts.Find(filter).FirstOrDefaultAsync();
//             
//             if (post == null)
//             {
//                 _logger.LogWarning("Post with id {Id} not found", postId);
//                 throw new PostNotFoundException($"Post with id {postId} not found");
//             }
//
//             _logger.LogInformation("Post with id {Id} was found", postId);
//             return PostConverter.ConvertMongo(post);
//         }
//         catch (PostNotFoundException e)
//         {
//             _logger.LogWarning(e, $"Post with id {postId} not found");
//             throw;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error getting post with id - {postId}");
//             throw;
//         }
//     }
//
//     public async Task<BasePost> UpdatePostAsync(UpdatePost post)
//     {
//         try
//         {
//             var filter = Builders<PostMongoDb>.Filter.And(
//                 Builders<PostMongoDb>.Filter.Eq(p => p.Id, post.Id),
//                 Builders<PostMongoDb>.Filter.Eq(p => p.IsDeleted, false)
//             );
//
//             var postToUpdate = await _posts.Find(filter).FirstOrDefaultAsync();
//             if (postToUpdate == null)
//                 throw new PostNotFoundException($"Post with id {post.Id} not found");
//
//             // Проверка на дубликат у других постов
//             var duplicateFilter = Builders<PostMongoDb>.Filter.And(
//                 Builders<PostMongoDb>.Filter.Ne(p => p.Id, post.Id),
//                 Builders<PostMongoDb>.Filter.Eq(p => p.CompanyId, postToUpdate.CompanyId),
//                 Builders<PostMongoDb>.Filter.Eq(p => p.Title, post.Title),
//                 Builders<PostMongoDb>.Filter.Eq(p => p.IsDeleted, false)
//             );
//
//             var existingPost = await _posts.Find(duplicateFilter).FirstOrDefaultAsync();
//             if (existingPost != null)
//                 throw new PostAlreadyExistsException(
//                     $"Post with title {post.Title} already exists in company {postToUpdate.CompanyId}");
//
//             // Подготовка обновлений
//             var updateDefinition = Builders<PostMongoDb>.Update
//                 .Set(p => p.UpdatedAt, DateTime.UtcNow);
//
//             if (post.Title != null) 
//                 updateDefinition = updateDefinition.Set(p => p.Title, post.Title);
//             if (post.Salary.HasValue) 
//                 updateDefinition = updateDefinition.Set(p => p.Salary, post.Salary.Value);
//
//             var options = new FindOneAndUpdateOptions<PostMongoDb> 
//             { 
//                 ReturnDocument = ReturnDocument.After 
//             };
//
//             var updatedPost = await _posts.FindOneAndUpdateAsync(
//                 filter, updateDefinition, options);
//
//             if (updatedPost == null)
//                 throw new PostNotFoundException($"Post with id {post.Id} not found after update");
//
//             _logger.LogInformation("Post with id {Id} was updated", post.Id);
//             return PostConverter.ConvertMongo(updatedPost);
//         }
//         catch (PostNotFoundException e)
//         {
//             _logger.LogWarning(e, $"Post with id {post.Id} not found");
//             throw;
//         }
//         catch (PostAlreadyExistsException e)
//         {
//             _logger.LogWarning(e, "Post with same title already exists");
//             throw;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error updating post with id - {post.Id}");
//             throw;
//         }
//     }
//
//     public async Task<IEnumerable<BasePost>> GetPostsAsync(Guid companyId)
//     {
//         try
//         {
//             var filter = Builders<PostMongoDb>.Filter.And(
//                 Builders<PostMongoDb>.Filter.Eq(p => p.CompanyId, companyId),
//                 Builders<PostMongoDb>.Filter.Eq(p => p.IsDeleted, false)
//             );
//             
//             var sort = Builders<PostMongoDb>.Sort.Ascending(p => p.Title);
//
//             var posts = await _posts
//                 .Find(filter)
//                 .Sort(sort)
//                 .ToListAsync();
//
//             var basePosts = posts.Select(PostConverter.ConvertMongo).ToList();
//
//             _logger.LogInformation("Got {Count} posts for company {CompanyId}", basePosts.Count, companyId);
//             return basePosts;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error getting posts for company - {companyId}");
//             throw;
//         }
//     }
//
//     public async Task DeletePostAsync(Guid postId)
//     {
//         try
//         {
//             var filter = Builders<PostMongoDb>.Filter.And(
//                 Builders<PostMongoDb>.Filter.Eq(p => p.Id, postId),
//                 Builders<PostMongoDb>.Filter.Eq(p => p.IsDeleted, false)
//             );
//
//             var post = await _posts.Find(filter).FirstOrDefaultAsync();
//             if (post == null)
//                 throw new PostNotFoundException($"Post with id {postId} not found");
//
//             // Помечаем пост как удаленный
//             var updateDefinition = Builders<PostMongoDb>.Update
//                 .Set(p => p.IsDeleted, true)
//                 .Set(p => p.UpdatedAt, DateTime.UtcNow);
//
//             var result = await _posts.UpdateOneAsync(filter, updateDefinition);
//
//             if (result.ModifiedCount == 0)
//                 throw new PostNotFoundException($"Post with id {postId} not found for deletion");
//
//             _logger.LogInformation("Post with id {Id} was deleted", postId);
//         }
//         catch (PostNotFoundException e)
//         {
//             _logger.LogWarning(e, $"Post with id {postId} not found for deletion");
//             throw;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, $"Error deleting post with id - {postId}");
//             throw;
//         }
//     }
// }
