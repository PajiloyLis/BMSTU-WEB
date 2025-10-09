using Database.Context.Configuration;
using Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Project.Database.Context.Configuration;
using Project.Database.Models;

namespace Database.Context;

/// <summary>
///     Database context.
/// </summary>
public class ProjectDbContext : DbContext
{
    public ProjectDbContext(DbContextOptions<ProjectDbContext> options) : base(options)
    {
    }

    protected ProjectDbContext()
    {
    }

    public DbSet<EmployeeDb> EmployeeDb { get; set; }

    public DbSet<CompanyDb> CompanyDb { get; set; }

    public DbSet<EducationDb> EducationDb { get; set; }

    public DbSet<PostDb> PostDb { get; set; }

    public DbSet<PositionDb> PositionDb { get; set; }

    public DbSet<ScoreDb> ScoreDb { get; set; }

    public DbSet<PostHistoryDb> PostHistoryDb { get; set; }

    public DbSet<PositionHistoryDb> PositionHistoryDb { get; set; }

    public DbSet<UserDb> UserDb { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<PositionHierarchyWithEmployeeIdDb>(entity =>
        {
            entity.HasNoKey();
            entity.ToView(null);
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.Level).HasColumnName("level");
            entity.Property(e => e.PositionId).HasColumnName("position_id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
        });

        modelBuilder.Entity<PositionHierarchyDb>(entity =>
            {
                entity.HasNoKey();
                entity.ToView(null);
                entity.Property(e => e.Level).HasColumnName("level");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.PositionId).HasColumnName("id");
                entity.Property(e => e.ParentId).HasColumnName("parent_id");
            }
        );

        // modelBuilder.HasDbFunction(
        //     typeof(ProjectDbContext)
        //         .GetMethod(nameof(GetCurrentSubordinatesIdByEmployeeId))!);
        //
        // modelBuilder.HasDbFunction(() => GetCurrentSubordinatesIdByEmployeeId(Guid.Empty));
        //
        // modelBuilder.HasDbFunction(typeof(ProjectDbContext).GetMethod(nameof(GetSubordinatesById))!);
        //
        // modelBuilder.HasDbFunction(() => GetSubordinatesById(Guid.Empty));

        modelBuilder.ApplyConfiguration(new EmployeeDbConfiguration());
        modelBuilder.ApplyConfiguration(new CompanyDbConfiguration());
        modelBuilder.ApplyConfiguration(new EducationDbConfiguration());
        modelBuilder.ApplyConfiguration(new PostDbConfiguration());
        modelBuilder.ApplyConfiguration(new PositionDbConfiguration());
        modelBuilder.ApplyConfiguration(new PostHistoryDbConfiguration());
        modelBuilder.ApplyConfiguration(new ScoreDbConfiguration());
        modelBuilder.ApplyConfiguration(new PositionHistoryDbConfiguration());
        modelBuilder.ApplyConfiguration(new UserDbConfiguration());
    }

    // [DbFunction(Name = "get_current_subordinates_id_by_employee_id", Schema = "public")]
    // public IQueryable<PositionHierarchyWithEmployeeIdDb> GetCurrentSubordinatesIdByEmployeeId(Guid startId)
    // {
    //     return Set<PositionHierarchyWithEmployeeIdDb>()
    //         .FromSqlRaw("SELECT * FROM get_current_subordinates_id_by_employee_id({0})", startId);
    // }
    //
    // [DbFunction(Name = "get_subordinates_by_id", Schema = "public")]
    // public IQueryable<PositionHierarchyDb> GetSubordinatesById(Guid startId)
    // {
    //     return Set<PositionHierarchyDb>()
    //         .FromSqlRaw("SELECT * FROM get_subordinates_by_id({0})", startId);
    // }
}

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<CompanyMongoDb> Companies => _database.GetCollection<CompanyMongoDb>("companies");
    public IMongoCollection<PostMongoDb> Posts => _database.GetCollection<PostMongoDb>("posts");
    public IMongoCollection<PositionMongoDb> Positions => _database.GetCollection<PositionMongoDb>("positions");
    public IMongoCollection<EmployeeMongoDb> Employees => _database.GetCollection<EmployeeMongoDb>("employees");
    public IMongoCollection<EducationMongoDb> Educations => _database.GetCollection<EducationMongoDb>("educations");
    public IMongoCollection<ScoreMongoDb> Scores => _database.GetCollection<ScoreMongoDb>("scores");
    public IMongoCollection<PostHistoryMongoDb> PostHistories => _database.GetCollection<PostHistoryMongoDb>("postHistories");
    public IMongoCollection<PositionHistoryMongoDb> PositionHistories => _database.GetCollection<PositionHistoryMongoDb>("positionHistories");
    public IMongoCollection<UserMongoDb> Users => _database.GetCollection<UserMongoDb>("users");
}

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}