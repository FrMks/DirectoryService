using CSharpFunctionalExtensions;
using FileService.Contracts;
using Shared;

namespace FileService.Communication;

public interface IFileCommunicationService
{
    Task<Result<FileResponse, Errors>> GetMideAssetById(
        Guid mediaAssetId,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<FileResponse>, Errors>> GetFilesByOwnerAsync(
        string context,
        Guid entityID,
        CancellationToken cancellationToken);

    Task<Result<GetContentUrlResponse, Errors>> GetContentUrlAsync(
        Guid fileId,
        CancellationToken cancellationToken);
}
