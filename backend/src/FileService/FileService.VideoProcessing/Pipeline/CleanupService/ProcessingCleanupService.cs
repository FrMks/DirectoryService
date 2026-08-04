using CSharpFunctionalExtensions;
using FileService.Core.Multipart;
using FileService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Shared;

namespace FileService.VideoProcessing.Pipeline.CleanupService;

public class ProcessingCleanupService : IProcessingCleanupService
{
    private readonly IS3Provider _s3Provider;
    private readonly ILogger<ProcessingCleanupService> _logger;

    public ProcessingCleanupService(
        IS3Provider s3Provider,
        ILogger<ProcessingCleanupService> logger)
    {
        _s3Provider = s3Provider;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> CleanupUploadedSourceAsync(ProcessingContext context, CancellationToken cancellationToken = default)
    {
        StorageKey? uploadedKey = context.VideoAsset.UploadedKey;
        if (uploadedKey is null)
        {
            _logger.LogWarning(
                "Uploaded key is not set for VideoAssetId: {VideoAssetId}, skipping cleanup of uploaded source",
                context.VideoAsset.Id);
            return UnitResult.Failure(Error.Failure(
                "uploaded.key.not.set",
                "Uploaded key is not set in the video asset."));
        }

        Result<string, Error> deleteResult = await _s3Provider.DeleteFileAsync(uploadedKey, cancellationToken);
        if (deleteResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to delete uploaded source from storage for VideoAssetId: {VideoAssetId}. Error: {Error}",
                context.VideoAsset.Id,
                deleteResult.Error);
            return UnitResult.Failure(deleteResult.Error);
        }

        _logger.LogDebug(
            "Uploaded source deleted from storage for VideoAssetId: {VideoAssetId}",
            context.VideoAsset.Id);

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> CleanupWorkingDirectory(ProcessingContext context)
    {
        if (string.IsNullOrWhiteSpace(context.WorkingDirectory))
        {
            return UnitResult.Failure(Error.Failure(
                "working.directory.not.set",
                "Working directory is not set in the processing context."));
        }

        try
        {
            if (Directory.Exists(context.WorkingDirectory))
            {
                Directory.Delete(context.WorkingDirectory, recursive: true);
                _logger.LogDebug(
                    "Working directory deleted: {WorkingDirectory}",
                    context.WorkingDirectory);
            }

            context.Cleanup();

            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to delete working directory: {WorkingDirectory} for VideoAssetId: {VideoAssetId}",
                context.WorkingDirectory,
                context.VideoAsset.Id);
            return UnitResult.Failure(Error.Failure(
                "working.directory.deletion.failed",
                $"Failed to delete working directory: {context.WorkingDirectory}"));
        }
    }
}