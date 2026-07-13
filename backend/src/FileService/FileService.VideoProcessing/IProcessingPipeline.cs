using CSharpFunctionalExtensions;
using Shared;

namespace FileService.VideoProcessing;

public interface IProcessingPipeline
{
    Task<UnitResult<Error>> ProcessAllStepsAsync(Guid videoAssetId, CancellationToken cancellationToken = default);
}