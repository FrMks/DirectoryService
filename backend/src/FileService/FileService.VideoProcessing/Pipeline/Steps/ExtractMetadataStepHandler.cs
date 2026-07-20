using CSharpFunctionalExtensions;
using FileService.Domain.MediaProcessing;
using FileService.Domain.ValueObjects;
using Microsoft.AspNetCore.Routing.Internal;
using Shared;

namespace FileService.VideoProcessing.Pipeline.Steps;

public class ExtractMetadataStepHandler : IProcessingStepHandler
{
    public StepType StepType => StepType.EXTRACT_METADATA;

    public Task<Result<ProcessingContext, Error>> ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken = default)
    {


        if (cancellationToken.IsCancellationRequested)
        {
            var error = Error.Failure("processing.cancelled", "Video processing was cancelled");
            return Task.FromResult(Result.Failure<ProcessingContext, Error>(error));
        }

        Result<VideoMetadata, Error> videoMetadataResult = VideoMetadata.Create(
            TimeSpan.FromSeconds(120),
            1920,
            1080,
            "h264",
            "mp4");
        if (videoMetadataResult.IsFailure)
            return Task.FromResult(Result.Failure<ProcessingContext, Error>(videoMetadataResult.Error));

        UnitResult<Error> setMetadataResult = context.VideoAsset.SetMetadata(videoMetadataResult.Value);
        if (setMetadataResult.IsFailure)
            return Task.FromResult(Result.Failure<ProcessingContext, Error>(setMetadataResult.Error));

        return Task.FromResult(Result.Success<ProcessingContext, Error>(context));
    }
}
