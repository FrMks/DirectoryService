using DirectoryService.Infrastructure.Postgres.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DirectoryService.Infrastructure.Postgres.DepartmentCleanupBackgroundService;

public class DepartmentCleanupBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<DepartmentCleanupBackgroundService> _logger;
    private readonly IOptions<DepartmentCleanupOptions> _options;

    public DepartmentCleanupBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<DepartmentCleanupBackgroundService> logger,
        IOptions<DepartmentCleanupOptions> options)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogInformation("Department cleanup background service is disabled. Exiting.");
            return;
        }

        _logger.LogInformation("Department cleanup background service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _serviceScopeFactory.CreateAsyncScope();
                var cleanupService = scope.ServiceProvider.GetRequiredService<DepartmentCleanupService>();
                await cleanupService.CleanupInactiveDepartments(_options.Value.InactiveDaysThreshold, stoppingToken);

                _logger.LogInformation("Department cleanup completed. Waiting for the next interval.");
                await Task.Delay(TimeSpan.FromHours(_options.Value.IntervalHours), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while cleaning up inactive departments.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
