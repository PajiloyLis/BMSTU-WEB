using Microsoft.Extensions.DependencyInjection;
using Project.Core.Repositories;
using Project.Database.Repositories;

namespace Database.Repositories.Extensions;

public static class DbRepositoriesProvider
{
    public static IServiceCollection AddDbRepositories(this IServiceCollection services)
    {
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IEducationRepository, EducationRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();
        services.AddScoped<IScoreRepository, ScoreRepository>();
        services.AddScoped<IPostHistoryRepository, PostHistoryRepository>();
        services.AddScoped<IPositionHistoryRepository, PositionHistoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }
    
    // public static IServiceCollection AddMongoDbRepositories(this IServiceCollection services)
    // {
    //     services.AddScoped<IEmployeeRepository, EmployeeRepositoryMongo>();
    //     services.AddScoped<ICompanyRepository, CompanyRepositoryMongo>();
    //     services.AddScoped<IEducationRepository, EducationRepositoryMongo>();
    //     services.AddScoped<IPostRepository, PostRepositoryMongo>();
    //     services.AddScoped<IPositionRepository, PositionRepositoryMongo>();
    //     services.AddScoped<IScoreRepository, ScoreRepositoryMongo>();
    //     services.AddScoped<IPostHistoryRepository, PostHistoryRepositoryMongo>();
    //     services.AddScoped<IPositionHistoryRepository, PositionHistoryRepositoryMongo>();
    //     services.AddScoped<IUserRepository, UserRepositoryMongo>();
    //     return services;
    // }
}