using CSharpFunctionalExtensions;
using Shared;

namespace FileService.Core.Outbox;

public interface IProcessingRetryOutboxRepository
{
    Task<UnitResult<Error>> CreateAsync(
        Guid videoAssetId,
        int retryCount,
        CancellationToken cancellationToken);
}
