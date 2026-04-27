using Microsoft.EntityFrameworkCore;
using PipelineEval.Api.Data;

namespace PipelineEval.Api.Services;

/// <summary>
/// Runs <see cref="DatabaseFacade.EnsureCreatedAsync"/> after the web host is listening so load balancer
/// health checks are not blocked by slow or flaky first-time DB connectivity.
/// </summary>
public sealed class DbSchemaInitializer : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DbSchemaInitializer> _logger;

    public DbSchemaInitializer(IServiceProvider services, ILogger<DbSchemaInitializer> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync(stoppingToken).ConfigureAwait(false);
            _logger.LogInformation("Database schema ensured.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Database schema initialization failed; API may return errors until DB is reachable.");
        }
    }
}
