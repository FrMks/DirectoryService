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

public sealed class ProcessingRetryOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptions<OutboxOptions> _options;
    private readonly ILogger<ProcessingRetryOutboxWorker> _logger;

    public ProcessingRetryOutboxWorker(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<OutboxOptions> options,
        ILogger<ProcessingRetryOutboxWorker> logger)
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

                Result<IReadOnlyList<ProcessingRetryOutboxMessage>, Error> handleBatchResult = await HandleBatchFromOutbox(
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

    private async Task<Result<IReadOnlyList<ProcessingRetryOutboxMessage>, Error>> HandleBatchFromOutbox(
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

        IReadOnlyList<ProcessingRetryOutboxMessage> messages = await FindMessageOnPendingAndFailedStatus(
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

        return Result.Success<IReadOnlyList<ProcessingRetryOutboxMessage>, Error>(messages);
    }

    private async Task<IReadOnlyList<ProcessingRetryOutboxMessage>> FindMessageOnPendingAndFailedStatus(
        FileServiceDbContext dbContext,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        int batchSize = _options.Value.BatchSize;

        return await dbContext.ProcessingRetryOutboxMessage
            .FromSqlInterpolated($"""
                SELECT *
                FROM processing_retry_outbox_messages
                WHERE status IN ('Pending', 'Failed')
                    AND next_attempt_at <= {now}
                    AND attempts < max_retries
                ORDER BY created_at
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
            """)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    private async Task<UnitResult<Error>> HandleMessages(
        IReadOnlyList<ProcessingRetryOutboxMessage> messages,
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        IProcessingRetryScheduler processingRetryScheduler =
            scope.ServiceProvider.GetRequiredService<IProcessingRetryScheduler>();

        foreach (ProcessingRetryOutboxMessage message in messages)
        {
            try
            {
                await processingRetryScheduler.ScheduleAsync(
                    message.VideoAssetId,
                    message.RetryCount,
                    message.NextAttemptAt.UtcDateTime,
                    cancellationToken);

                message.MarkCompleted();
            }
            catch (ObjectAlreadyExistsException ex)
            {
                _logger.LogInformation(ex, "Job already exist in Quartz");
                message.MarkCompleted();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to schedule processing job for VideoAssetId: {VideoAssetId}",
                    message.VideoAssetId);

                message.MarkFailed(ex.Message, _options.Value.InitialRetryDelaySeconds);
            }
        }

        return await SaveMessageStatusesAsync(scope, cancellationToken);
    }

    private async Task<UnitResult<Error>> SaveMessageStatusesAsync(
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        ITransactionManager transactionManager =
            scope.ServiceProvider.GetRequiredService<ITransactionManager>();

        Result<ITransactionScope, Error> beginTransactionResult =
            await transactionManager.BeginTransaction(cancellationToken);
        if (beginTransactionResult.IsFailure)
        {
            return beginTransactionResult.Error;
        }

        using ITransactionScope transaction = beginTransactionResult.Value;

        UnitResult<Error> saveChangesResult =
            await transactionManager.SaveChangesAsync(cancellationToken);
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

        return Result.Success<Error>();
    }
}
