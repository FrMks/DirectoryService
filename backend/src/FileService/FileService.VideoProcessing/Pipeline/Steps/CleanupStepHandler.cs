using CSharpFunctionalExtensions;
using FileService.Core.Multipart;
using FileService.Domain.MediaProcessing;
using Microsoft.Extensions.Logging;
using Shared;

namespace FileService.VideoProcessing.Pipeline.Steps;

public class CleanupStepHandler : IProcessingStepHandler
{
    private readonly ILogger<CleanupStepHandler> _logger;
    private readonly IS3Provider _s3Provider;

    public CleanupStepHandler(ILogger<CleanupStepHandler> logger, IS3Provider s3Provider)
    {
        _logger = logger;
        _s3Provider = s3Provider;
    }

    public StepType StepType => StepType.CLEANUP;

    public async Task<Result<ProcessingContext, Error>> ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Cleaning up temporary files for VideoAssetId: {VideoAssetId}",
            context.VideoAsset.Id);

        if (string.IsNullOrWhiteSpace(context.WorkingDirectory))
        {
            _logger.LogWarning("Working directory is not set, skipping cleanup");
            return await Task.FromResult(context);
        }

        UnitResult<Error> deleteResult = await _s3Provider
            .DeleteFileAsync(context.VideoAsset.UploadedKey!, cancellationToken);
        if (deleteResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to delete raw file from storage for VideoAssetId: {VideoAssetId}. Error: {Error}",
                context.VideoAsset.Id,
                deleteResult.Error);
        }
        else
        {
            _logger.LogDebug(
                "Raw file deleted from storage for VideoAssetId: {VideoAssetId}",
                context.VideoAsset.Id);
        }

        try
        {
            if (Directory.Exists(context.WorkingDirectory))
            {
                Directory.Delete(context.WorkingDirectory, recursive: true);
                _logger.LogDebug(
                    "Working directory deleted: {WorkingDirectory}",
                    context.WorkingDirectory);

                context.Cleanup();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to delete working directory: {WorkingDirectory}. Will be cleaned up later.",
                context.WorkingDirectory);
        }

        return await Task.FromResult(context);
    }
}
