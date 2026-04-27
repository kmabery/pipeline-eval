using Microsoft.EntityFrameworkCore;
using PipelineEval.Api.Data;
using PipelineEval.Api.Services;

namespace PipelineEval.Api.Startup;

/// <summary>
/// Single DI composition root. Mirrors the <c>nextService</c> CLI template's <c>ServiceRegistration</c>
/// while keeping our project-specific bindings: EF Core (Postgres or in-memory for the integration test
/// host), CORS, health checks, hosted bootstrappers, S3 / local cat-picture storage selector, and the
/// new <see cref="ITodoService"/> / <see cref="IInviteService"/> abstractions.
/// </summary>
public static class ServiceRegistration
{
    public static WebApplicationBuilder InjectServiceDependencies(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;
        var environment = builder.Environment;

        services.AddHealthChecks();
        services.AddHostedService<DbSchemaInitializer>();
        services.AddHostedService<S3LocalDevInitializer>();

        if (environment.IsEnvironment("Testing"))
        {
            var inMemoryName = configuration["Testing:InMemoryDatabaseName"] ?? "PipelineEvalIntegration";
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(inMemoryName));
        }
        else
        {
            var connectionString = ConnectionStringHelper.GetPipelineEval(configuration);
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        }

        services.AddSingleton<ICatPictureStorage>(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var env = sp.GetRequiredService<IHostEnvironment>();
            var root = cfg["LocalStorage:RootPath"];
            if (!string.IsNullOrWhiteSpace(root) && env.IsDevelopment())
                return new LocalDirectoryCatPictureStorage(cfg, env);
            return new CatPictureStorage(cfg);
        });

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (environment.IsDevelopment())
                {
                    policy.SetIsOriginAllowed(_ => true)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
                else
                {
                    var origins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
                    policy.WithOrigins(origins.Length > 0 ? origins : ["https://localhost"])
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
            });
        });

        services.AddScoped<ITodoService, TodoService>();

        return builder;
    }
}
