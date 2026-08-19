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

                UnitResult<Error> handleBatchResult = await HandleBatchFromOutbox(scope, stoppingToken);
                if (handleBatchResult.IsFailure)
                {
                    _logger.LogError("Has error: {ErrorMessage} when try handle batch.", handleBatchResult.Error.Message);
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

    private async Task<UnitResult<Error>> HandleBatchFromOutbox(IServiceScope scope, CancellationToken cancellationToken)
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

        return Result.Success<Error>();
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
}