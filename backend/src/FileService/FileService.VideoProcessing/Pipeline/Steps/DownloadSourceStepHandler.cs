using CSharpFunctionalExtensions;
using FileService.Core.Multipart;
using FileService.Domain.Errors;
using FileService.Domain.MediaProcessing;
using FileService.Domain.ValueObjects;
using Shared;

namespace FileService.VideoProcessing.Pipeline.Steps;

public class DownloadSourceStepHandler : IProcessingStepHandler
{
    private readonly IS3Provider _s3Provider;

    public DownloadSourceStepHandler(IS3Provider s3Provider)
    {
        _s3Provider = s3Provider;
    }

    public StepType StepType => StepType.DOWNLOAD_SOURCE;

    public async Task<Result<ProcessingContext, Error>> ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.WorkingDirectory is null)
        {
            return Result.Failure<ProcessingContext, Error>(
                Error.Failure(
                    "working.directory.not.set",
                    "Working directory is not set in the processing context."));
        }

        StorageKey? uploadedKey = context.VideoAsset.UploadedKey;
        if (uploadedKey is null)
        {
            return Result.Failure<ProcessingContext, Error>(
                Error.Failure(
                    "uploaded.key.not.set",
                    "Uploaded key is not set in the video asset."));
        }

        string sourceFilePath = Path.Combine(context.WorkingDirectory, "source.mp4");

        Result<string, Error> downloadResult = await _s3Provider.DownloadFileAsync(
            uploadedKey,
            sourceFilePath,
            cancellationToken);
        if (downloadResult.IsFailure)
        {
            return Result.Failure<ProcessingContext, Error>(downloadResult.Error);
        }

        context.SetSourceFilePath(downloadResult.Value);

        return context;
    }
}
