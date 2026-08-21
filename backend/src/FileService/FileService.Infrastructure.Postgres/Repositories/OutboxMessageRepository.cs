using CSharpFunctionalExtensions;
using FileService.Core.Outbox;
using FileService.Domain.Outbox;
using FileService.Infrastructure.Postgres.Outbox;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;

namespace FileService.Infrastructure.Postgres.Repositories;

public class OutboxMessageRepository(
    FileServiceDbContext dbContext,
    IOptions<OutboxOptions> options,
    ILogger<OutboxMessageRepository> logger) : IOutboxMessageRepository
{
    public async Task<UnitResult<Error>> CreateAsync(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        ProcessingJobOutboxMessage processingJobOutboxMessage = ProcessingJobOutboxMessage
            .Create(mediaAssetId: mediaAssetId, options.Value.MaxRetries);
        if (processingJobOutboxMessage is null)
        {
            logger.LogError("Has error when create new Processing job outbox message");
            return Error.Failure("outbox.message.is.null", "Has error when create new Processing job outbox message");
        }

        await dbContext.ProcessingJobOutboxMessage.AddAsync(processingJobOutboxMessage, cancellationToken);
        return Result.Success<Error>();
    }
}