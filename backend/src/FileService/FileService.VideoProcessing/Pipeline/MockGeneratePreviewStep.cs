using CSharpFunctionalExtensions;
using FileService.Domain.MediaProcessing;
using Shared;

namespace FileService.VideoProcessing.Pipeline;

public class MockGeneratePreviewStep : IProcessingStepHandler
{
    public StepType StepType => StepType.GENERATE_PREVIEW;

    public Task<Result<ProcessingContext, Error>> ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            var error = Error.Failure("processing.cancelled", "Video processing was cancelled");
            return Task.FromResult(Result.Failure<ProcessingContext, Error>(error));
        }

        return Task.FromResult(Result.Success<ProcessingContext, Error>(context));
    }
}
