using CSharpFunctionalExtensions;
using FileService.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Core.Database;

namespace FileService.Infrastructure.Postgres.Outbox;

public class ProcessingJobOutboxRecoveryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptions<OutboxOptions> _options;
    private readonly ILogger<ProcessingJobOutboxRecoveryWorker> _logger;

    public ProcessingJobOutboxRecoveryWorker(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<OutboxOptions> options,
        ILogger<ProcessingJobOutboxRecoveryWorker> logger)
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

                Result<List<ProcessingJobOutboxMessage>, Error> messagesResult = await GetMessagesWithProcessingStatus(
                    scope,
                    stoppingToken);
                if (messagesResult.IsFailure)
                {
                    _logger.LogError("Error: {MessageError} was occured when try to get message with processing status", messagesResult.Error);
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
                    "Error occurred in processing job outbox recovery worker");

                await Task.Delay(
                    TimeSpan.FromSeconds(1),
                    stoppingToken);
            }
        }
    }

    private async Task<Result<List<ProcessingJobOutboxMessage>, Error>> GetMessagesWithProcessingStatus(
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
        int batchSize = _options.Value.BatchSize;
        DateTimeOffset cutoffTime = DateTimeOffset.UtcNow.AddSeconds(-_options.Value.ProcessingTimeoutSeconds);

        List<ProcessingJobOutboxMessage> processingJobOutboxMessages = await dbContext.ProcessingJobOutboxMessage
            .FromSqlInterpolated($"""
                SELECT *
                FROM processing_job_outbox_messages
                WHERE status = 'Processing'
                    AND started_processing_at <= {cutoffTime}
                ORDER BY created_at
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
            """)
            .ToListAsync(cancellationToken);

        processingJobOutboxMessages.ForEach(message => message.ResetProcessingToPending());

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

        return Result.Success<List<ProcessingJobOutboxMessage>, Error>(processingJobOutboxMessages);
    }
}