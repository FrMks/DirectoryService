using FileService.Core.Processing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.Postgres.Outbox;

public sealed class ProcessingRetryOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptions<OutboxOptions> _options;
    private readonly ILogger<ProcessingRetryOutboxWorker> _logger;
    private readonly IProcessingRetryScheduler _processingRetryScheduler;

    public ProcessingRetryOutboxWorker(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<OutboxOptions> options,
        ILogger<ProcessingRetryOutboxWorker> logger,
        IProcessingRetryScheduler processingRetryScheduler)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = options;
        _logger = logger;
        _processingRetryScheduler = processingRetryScheduler;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
