using CSharpFunctionalExtensions;
using FileService.Domain;

namespace FileService.Core;

public interface IMediaRepository
{
    Task AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken);

    Task<MediaAsset?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task UpdateAsync(MediaAsset mediaAsset, CancellationToken cancellationToken);
}