using Project.HttpServer.Extensions;

namespace Project.HttpServer;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddProjectControllers()
            .AddProjectSwaggerGen()
            .AddProjectDbRepositories()
            // .AddProjectMongoDbRepositories()
            .AddHttpContextAccessor()
            .AddProjectAuthorization(Configuration)
            .AddProjectCors(Configuration.GetValue("AllowedHeaders", "AllowAllHeaders"))
            .AddProjectDbContext(Configuration)
            // .AddProjectMongoDbContext(Configuration)
            .AddProjectServices(Configuration);

        // Конфигурация для загрузки файлов
        services.Configure<IISServerOptions>(options =>
        {
            options.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

        app.UseSwagger();
        app.UseSwaggerUI(options => { options.SwaggerEndpoint("/swagger/v1/swagger.json", "Project.HttpServer v1"); });

        // Добавляем поддержку статических файлов для фотографий
        var uploadsPath = Path.Combine(env.ContentRootPath, "uploads");
        if (!Directory.Exists(uploadsPath))
        {
            Directory.CreateDirectory(uploadsPath);
        }
        
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
            RequestPath = "/uploads"
        });

        app.UseRouting();
        app.UseCors("AllowAllHeaders");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(builder => builder.MapControllers());
    }
}