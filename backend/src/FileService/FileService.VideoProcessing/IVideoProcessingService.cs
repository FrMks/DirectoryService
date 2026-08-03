using CSharpFunctionalExtensions;
using Shared;

namespace FileService.VideoProcessing;

public interface IVideoProcessingService
{
    Task<UnitResult<Error>> ProcessVideoAsync(
        Guid videoAssetId,
        CancellationToken cancellationToken = default);
}