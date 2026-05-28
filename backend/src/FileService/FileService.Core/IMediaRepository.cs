using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FileService.Domain;
using Shared;

namespace FileService.Core;

public interface IMediaRepository
{
    Task<Result<Guid, Error>> AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken);

    Task<MediaAsset?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<MediaAsset, Error>> GetBy(
        Expression<Func<MediaAsset, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<int> SaveAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(MediaAsset mediaAsset, CancellationToken cancellationToken);
}