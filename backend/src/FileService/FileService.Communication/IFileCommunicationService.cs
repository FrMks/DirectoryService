using CSharpFunctionalExtensions;
using FileService.Contracts;
using Shared;

namespace FileService.Communication;

public interface IFileCommunicationService
{
    Task<Result<FileResponse, Error>> GetMideAssetById(Guid mediaAssetId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<FileResponse>, Error>> GetFilesByOwnerAsync(
        string context,
        Guid entityID,
        CancellationToken cancellationToken);

    Task<Result<GetContentUrlResponse, Error>> GetContentUrlAsync(
        Guid fileId,
        CancellationToken cancellationToken);
}
