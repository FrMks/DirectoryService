using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FileService.Domain.MediaProcessing;
using Shared;

namespace FileService.Core;

public interface IVideoProcessingRepository
{
    Task<Result<VideoProcess, Error>> GetBy(
        Expression<Func<VideoProcess, bool>> predicate,
        CancellationToken cancellationToken = default);

    void Add(VideoProcess videoProcess);

    Task<Result<VideoProcess, Error>> GetSnapshotByVideoAssetId(
        Guid videoAssetId,
        CancellationToken cancellationToken = default);
}