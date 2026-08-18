using CSharpFunctionalExtensions;
using Shared;

namespace FileService.Core.Outbox;

public interface IOutboxMessageRepository
{
    Task<UnitResult<Error>> CreateAsync(Guid mediaAssetId, CancellationToken cancellationToken);
}