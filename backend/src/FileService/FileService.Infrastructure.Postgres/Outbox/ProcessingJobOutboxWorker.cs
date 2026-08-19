using CSharpFunctionalExtensions;
using FileService.Core;
using FileService.Core.Processing;
using FileService.Domain.Entities.MediaAssetEntity;
using FileService.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Shared;
using Shared.Core.Database;

namespace FileService.Infrastructure.Postgres.Outbox;

public class ProcessingJobOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptions<OutboxOptions> _options;
    private readonly ILogger<ProcessingJobOutboxWorker> _logger;

    public ProcessingJobOutboxWorker(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<OutboxOptions> options,
        ILogger<ProcessingJobOutboxWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = _serviceScopeFactory.CreateScope();

                Result<IReadOnlyList<ProcessingJobOutboxMessage>, Error> handleBatchResult = await HandleBatchFromOutbox(
                    scope,
                    stoppingToken);
                if (handleBatchResult.IsFailure)
                {
                    _logger.LogError("Has error: {ErrorMessage} when try handle batch.", handleBatchResult.Error.Message);
                }
                else
                {
                    UnitResult<Error> handleMessagesResult = await HandleMessages(
                        handleBatchResult.Value,
                        scope,
                        stoppingToken);
                    if (handleMessagesResult.IsFailure)
                    {
                        _logger.LogError("Has error: {ErrorMessage} when try handle messages", handleMessagesResult.Error);
                    }
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(1),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while processing outbox messages");

                await Task.Delay(
                    TimeSpan.FromSeconds(1),
                    stoppingToken);
            }
        }
    }

    private async Task<Result<IReadOnlyList<ProcessingJobOutboxMessage>, Error>> HandleBatchFromOutbox(
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        FileServiceDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<FileServiceDbContext>();
        ITransactionManager transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();

        Result<ITransactionScope, Error> beginTransactionResult = await transactionManager.BeginTransaction(cancellationToken);
        if (beginTransactionResult.IsFailure)
        {
            return beginTransactionResult.Error;
        }

        using ITransactionScope transaction = beginTransactionResult.Value;

        IReadOnlyList<ProcessingJobOutboxMessage> messages = await FindMessageOnPendingAndFailedStatus(
            dbContext,
            cancellationToken);

        messages.ToList().ForEach(message => message.SwitchStatusTo(ProcessingJobOutboxStatus.Processing));
        messages.ToList().ForEach(message => message.IncrementAttempts());
        messages.ToList().ForEach(message => message.SetTimeWhenStartProcessing());

        UnitResult<Error> saveChangesResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveChangesResult.IsFailure)
        {
            transaction.Rollback();
            return saveChangesResult.Error;
        }
        UnitResult<Error> commitResult = transaction.Commit();
        if (commitResult.IsFailure)
        {
            return commitResult.Error;
        }

        return Result.Success<IReadOnlyList<ProcessingJobOutboxMessage>, Error>(messages);
    }

    private async Task<IReadOnlyList<ProcessingJobOutboxMessage>> FindMessageOnPendingAndFailedStatus(
        FileServiceDbContext dbContext,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        int batchSize = _options.Value.BatchSize;

        return await dbContext.ProcessingJobOutboxMessage
            .FromSqlInterpolated($"""
                SELECT *
                FROM processing_job_outbox_messages
                WHERE status IN ('Pending', 'Failed')
                    AND next_attempt_at <= {now}
                ORDER BY created_at
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
            """)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    private async Task<UnitResult<Error>> HandleMessages(
        IReadOnlyList<ProcessingJobOutboxMessage> messages,
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        IMediaRepository mediaRepository = scope.ServiceProvider.GetRequiredService<IMediaRepository>();
        // Механизм, который принимает задания и запускает их в нужное время.
        IEnumerable<IProcessingJobFactory> processingJobFactories = scope.ServiceProvider.GetRequiredService<IEnumerable<IProcessingJobFactory>>();
        ISchedulerFactory schedulerFactory = scope.ServiceProvider.GetRequiredService<ISchedulerFactory>();

        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);

        foreach (ProcessingJobOutboxMessage message in messages)
        {
            Result<MediaAsset, Error> mediaAssetResult = await mediaRepository
                .GetBy(m => m.Id == message.MediaAssetId, cancellationToken);
            if (mediaAssetResult.IsFailure)
            {
                _logger.LogError("Media asset result is failure when try to get by media asset id {MediaAssetId}", message.MediaAssetId);
                return mediaAssetResult.Error;
            }

            MediaAsset mediaAsset = mediaAssetResult.Value;

            // Адаптер над Quartz. Worker не знает деталей конкретной обработки видео. Он нахоидит factory. Для Video это VideoProcessingJobFactory.
            IProcessingJobFactory? processingJobFactory = processingJobFactories.FirstOrDefault(f => f.CanProcess(mediaAsset));
            if (processingJobFactory is null)
            {
                _logger.LogError("No processing job factory found for MediaAssetId: {MediaAssetId}", mediaAsset.Id);
                return Error.Failure("processing.job.not.found", "No processing job factory found");
            }

            IJobDetail job = processingJobFactory.CreateJob(mediaAsset);
            ITrigger trigger = processingJobFactory.CreateTrigger(mediaAsset);

            await scheduler.ScheduleJob(job, trigger, cancellationToken);
        }

        return Result.Success<Error>();
    }
}