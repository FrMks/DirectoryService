using CSharpFunctionalExtensions;
using FileService.Domain.MediaProcessing;
using Shared;

namespace FileService.VideoProcessing.Pipeline;

public class MockPrepareOutputsStep : IProcessingStepHandler
{
    public StepType StepType => StepType.GENERATE_HLS;

    public Task<Result<ProcessingContext, Error>> ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            var error = Error.Failure("processing.cancelled", "Video processing was cancelled");
            return Task.FromResult(Result.Failure<ProcessingContext, Error>(error));
        }

        if (string.IsNullOrWhiteSpace(context.WorkingDirectory))
        {
            var error = Error.Failure("processing.working.directory.null", "WorkingDirectory is null");
            return Task.FromResult(Result.Failure<ProcessingContext, Error>(error));
        }

        if (string.IsNullOrWhiteSpace(context.HlsOutputDirectory))
        {
            var error = Error.Failure("processing.hls.output.directory.null", "HlsOutputDirectory is null");
            return Task.FromResult(Result.Failure<ProcessingContext, Error>(error));
        }

        return Task.FromResult(Result.Success<ProcessingContext, Error>(context));
    }
}
