using CSharpFunctionalExtensions;
using FileService.Core.Outbox;
using FileService.Domain.Outbox;
using FileService.Infrastructure.Postgres.Outbox;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;

namespace FileService.Infrastructure.Postgres.Repositories;

public sealed class ProcessingRetryOutboxRepository(
    FileServiceDbContext dbContext,
    IOptions<OutboxOptions> options,
    ILogger<ProcessingRetryOutboxRepository> logger) : IProcessingRetryOutboxRepository
{
    public async Task<UnitResult<Error>> CreateAsync(
        Guid videoAssetId,
        int retryCount,
        CancellationToken cancellationToken)
    {
        ProcessingRetryOutboxMessage message = ProcessingRetryOutboxMessage.Create(
            videoAssetId,
            retryCount,
            options.Value.MaxRetries);

        if (message is null)
        {
            logger.LogError(
                "Could not create processing retry outbox message for VideoAssetId {VideoAssetId}",
                videoAssetId);

            return Error.Failure(
                "processing.retry.outbox.message.is.null",
                "Could not create processing retry outbox message");
        }

        await dbContext.ProcessingRetryOutboxMessage.AddAsync(
            message,
            cancellationToken);

        return Result.Success<Error>();
    }
}
