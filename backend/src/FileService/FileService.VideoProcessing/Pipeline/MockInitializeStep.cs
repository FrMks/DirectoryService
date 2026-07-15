using CSharpFunctionalExtensions;
using FileService.Domain.MediaProcessing;
using Shared;

namespace FileService.VideoProcessing.Pipeline;

public class MockInitializeStep : IProcessingStepHandler
{
    public StepType StepType => StepType.INITIALIZE;

    public Task<Result<ProcessingContext, Error>> ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            var error = Error.Failure("processing.cancelled", "Video processing was cancelled");
            return Task.FromResult(Result.Failure<ProcessingContext, Error>(error));
        }

        if (context.VideoAsset is null)
        {
            var error = Error.Failure("video.asset.null", "Video asset is null in INITIALIZE");
            return Task.FromResult(Result.Failure<ProcessingContext, Error>(error));
        }

        if (context.VideoProcess is null)
        {
            var error = Error.Failure("video.process.null", "Video process is null in INITIALIZE");
            return Task.FromResult(Result.Failure<ProcessingContext, Error>(error));
        }

        if (string.IsNullOrWhiteSpace(context.WorkingDirectory) || string.IsNullOrEmpty(context.WorkingDirectory))
        {
            var error = Error.Failure("processing.working.directory.null", "WorkingDirectory is null");
            return Task.FromResult(Result.Failure<ProcessingContext, Error>(error));
        }

        if (string.IsNullOrWhiteSpace(context.HlsOutputDirectory) || string.IsNullOrEmpty(context.HlsOutputDirectory))
        {
            var error = Error.Failure("processing.upload.hls.output.directory.null", "HlsOutputDirectory is null");
            return Task.FromResult(Result.Failure<ProcessingContext, Error>(error));
        }

        return Task.FromResult(Result.Success<ProcessingContext, Error>(context));
    }
}