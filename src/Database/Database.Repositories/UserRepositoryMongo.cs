using Database.Context;
using Database.Models;
using Database.Models.Converters;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Project.Core.Exceptions;
using Project.Core.Models.User;
using Project.Core.Repositories;

namespace Database.Repositories;

public class UserRepositoryMongo : IUserRepository
{
    private readonly IMongoCollection<UserMongoDb> _users;
    private readonly ILogger<UserRepository> _logger;

    public UserRepositoryMongo(MongoDbContext context, ILogger<UserRepository> logger)
    {
        _users = context.Users;
        _logger = logger;
        
        // Создаем индексы для производительности
        // CreateIndexes();
    }

    private void CreateIndexes()
    {
        // Уникальный индекс для email
        _users.Indexes.CreateOne(
            new CreateIndexModel<UserMongoDb>(
                Builders<UserMongoDb>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true }
            ));

        // Индекс для поиска по роли
        _users.Indexes.CreateOne(
            new CreateIndexModel<UserMongoDb>(
                Builders<UserMongoDb>.IndexKeys.Ascending(u => u.Role)
            ));
    }

    public async Task<BaseUser> GetUserByEmailAsync(string email)
    {
        try
        {
            var filter = Builders<UserMongoDb>.Filter.Eq(u => u.Email, email);
            var user = await _users.Find(filter).FirstOrDefaultAsync();
            
            if (user == null)
            {
                _logger.LogWarning("User with email {Email} not found", email);
                throw new UserNotFoundException($"User with email {email} not found");
            }

            _logger.LogInformation("User with email {Email} was found", email);
            return UserConverter.ConvertMongo(user);
        }
        catch (UserNotFoundException e)
        {
            _logger.LogWarning(e, $"User with email {email} not found");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error getting user with email - {email}");
            throw;
        }
    }

    public async Task DeleteUserByIdAsync(string email)
    {
        try
        {
            var filter = Builders<UserMongoDb>.Filter.Eq(u => u.Email, email);
            var result = await _users.DeleteOneAsync(filter);

            if (result.DeletedCount == 0)
            {
                _logger.LogWarning("User with email {Email} not found for deletion", email);
                throw new UserNotFoundException($"User with email {email} not found");
            }

            _logger.LogInformation("User with email {Email} was deleted", email);
        }
        catch (UserNotFoundException e)
        {
            _logger.LogWarning(e, $"User with email {email} not found for deletion");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error deleting user with email - {email}");
            throw;
        }
    }

    public async Task UpdateUserByIdAsync(string emailOld, string passwordOld,
        string? passwordHash,
        string? email)
    {
        try
        {
            var filter = Builders<UserMongoDb>.Filter.And(
                Builders<UserMongoDb>.Filter.Eq(u => u.Email, emailOld),
                Builders<UserMongoDb>.Filter.Eq(u => u.Password, passwordOld)
            );

            var userToUpdate = await _users.Find(filter).FirstOrDefaultAsync();
            if (userToUpdate == null)
                throw new UserNotFoundException($"User with email {emailOld} and provided password not found");

            // Проверка на дубликат email, если email изменяется
            if (!string.IsNullOrEmpty(email) && email != emailOld)
            {
                var duplicateFilter = Builders<UserMongoDb>.Filter.Eq(u => u.Email, email);
                var existingUser = await _users.Find(duplicateFilter).FirstOrDefaultAsync();
                if (existingUser != null)
                    throw new UserAlreadyExistsException($"User with email {email} already exists");
            }

            // Подготовка обновлений
            var updateDefinition = Builders<UserMongoDb>.Update
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            if (!string.IsNullOrEmpty(passwordHash))
                updateDefinition = updateDefinition.Set(u => u.Password, passwordHash);
            if (!string.IsNullOrEmpty(email))
                updateDefinition = updateDefinition.Set(u => u.Email, email);

            var options = new FindOneAndUpdateOptions<UserMongoDb> 
            { 
                ReturnDocument = ReturnDocument.After 
            };

            var updatedUser = await _users.FindOneAndUpdateAsync(
                filter, updateDefinition, options);

            if (updatedUser == null)
                throw new UserNotFoundException($"User with email {emailOld} not found after update");

            _logger.LogInformation("User with email {EmailOld} was updated", emailOld);
        }
        catch (UserNotFoundException e)
        {
            _logger.LogWarning(e, $"User with email {emailOld} not found");
            throw;
        }
        catch (UserAlreadyExistsException e)
        {
            _logger.LogWarning(e, "User with same email already exists");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error updating user with email - {emailOld}");
            throw;
        }
    }

    public async Task<BaseUser> AddUserAsync(BaseUser user)
    {
        var userDb = UserConverter.ConvertMongo(user);
        userDb.CreatedAt = DateTime.UtcNow;
        userDb.UpdatedAt = DateTime.UtcNow;

        try
        {
            // Проверка на существующего пользователя с таким же email
            var existingUserFilter = Builders<UserMongoDb>.Filter.Eq(u => u.Email, userDb.Email);
            var existingUser = await _users.Find(existingUserFilter).FirstOrDefaultAsync();
            
            if (existingUser != null)
                throw new UserAlreadyExistsException($"User with email {userDb.Email} already exists");

            await _users.InsertOneAsync(userDb);
            
            _logger.LogInformation("User with email {Email} was added", userDb.Email);
            return UserConverter.ConvertMongo(userDb);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            _logger.LogError(ex, $"Duplicate key error creating user with email - {userDb.Email}");
            throw new UserAlreadyExistsException($"User with email {userDb.Email} already exists");
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error creating user with email - {userDb.Email}");
            throw;
        }
    }

    public async Task<Guid> GetCurrentUserIdAsync(string email)
    {
        try
        {
            var filter = Builders<UserMongoDb>.Filter.Eq(u => u.Email, email);
            var user = await _users.Find(filter).FirstOrDefaultAsync();
            
            if (user == null)
            {
                _logger.LogWarning("User with email {Email} not found", email);
                throw new UserNotFoundException($"User with email {email} not found");
            }

            _logger.LogInformation("Got user id {Id} for email {Email}", user.Id, email);
            return user.Id;
        }
        catch (UserNotFoundException e)
        {
            _logger.LogWarning(e, $"User with email {email} not found");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error getting user id for email - {email}");
            throw;
        }
    }
}
