using CSharpFunctionalExtensions;
using FileService.Domain.MediaProcessing;
using FileService.VideoProcessing.Pipeline.CleanupService;
using Microsoft.Extensions.Logging;
using Shared;

namespace FileService.VideoProcessing.Pipeline.Steps;

public class CleanupStepHandler : IProcessingStepHandler
{
    private readonly ILogger<CleanupStepHandler> _logger;
    private readonly IProcessingCleanupService _processingCleanupService;

    public CleanupStepHandler(
        ILogger<CleanupStepHandler> logger,
        IProcessingCleanupService processingCleanupService)
    {
        _logger = logger;
        _processingCleanupService = processingCleanupService;
    }

    public StepType StepType => StepType.CLEANUP;

    public async Task<Result<ProcessingContext, Error>> ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Cleaning up temporary files for VideoAssetId: {VideoAssetId}",
            context.VideoAsset.Id);

        UnitResult<Error> deleteResult = await _processingCleanupService
            .CleanupUploadedSourceAsync(context, cancellationToken);
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

        UnitResult<Error> workingDirectoryResult = _processingCleanupService.CleanupWorkingDirectory(context);
        if (workingDirectoryResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to cleanup working directory for VideoAssetId: {VideoAssetId}. Error: {Error}",
                context.VideoAsset.Id,
                workingDirectoryResult.Error);
        }

        return await Task.FromResult(context);
    }
}
