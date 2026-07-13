using CSharpFunctionalExtensions;
using Shared;

namespace FileService.VideoProcessing.Pipeline;

public interface IProcessingPipeline
{
    Task<UnitResult<Error>> ProcessAllStepsAsync(Guid videoAssetId, CancellationToken cancellationToken = default);
}