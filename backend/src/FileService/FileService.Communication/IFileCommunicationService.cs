using CSharpFunctionalExtensions;
using FileService.Contracts;
using Shared;

namespace FileService.Communication;

public interface IFileCommunicationService
{
    Task<Result<FileResponse, Errors>> GetMediaAssetByIdAsync(
        Guid mediaAssetId,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<FileResponse>, Errors>> GetFilesByOwnerAsync(
        string context,
        Guid entityId,
        CancellationToken cancellationToken);

    Task<Result<GetContentUrlResponse, Errors>> GetContentUrlAsync(
        Guid fileId,
        CancellationToken cancellationToken);
}
