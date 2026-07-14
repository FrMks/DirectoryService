using CSharpFunctionalExtensions;
using FileService.Domain.MediaProcessing;
using FileService.Domain.ValueObjects;
using Shared;

namespace FileService.VideoProcessing.Pipeline;

public class MockUploadResultsStep : IProcessingStepHandler
{
    // До upload hls файлы существуют локально во временной директории
    // после upload hls они должны оказаться в object storage
    public StepType StepType => StepType.UPLOAD_HLS;

    public Task<Result<ProcessingContext, Error>> ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            var error = Error.Failure("processing.cancelled", "Video processing was cancelled");
            return Task.FromResult(Result.Failure<ProcessingContext, Error>(error));
        }

        if (string.IsNullOrWhiteSpace(context.HlsOutputDirectory) || string.IsNullOrEmpty(context.HlsOutputDirectory))
        {
            var error = Error.Failure("processing.upload.hls.output.directory.null", "HlsOutputDirectory is null");
            return Task.FromResult(Result.Failure<ProcessingContext, Error>(error));
        }

        if (context.VideoAsset.HlsResult.ManifestKey == StorageKey.None)
        {
            var error = Error.Failure("processing.hls.result.manifest.key.null", "ManifestKey is null");
            return Task.FromResult(Result.Failure<ProcessingContext, Error>(error));
        }

        return Task.FromResult(Result.Success<ProcessingContext, Error>(context));
    }
}