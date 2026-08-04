using CSharpFunctionalExtensions;
using FileService.VideoProcessing.Pipeline;
using Microsoft.Extensions.Logging;
using Shared;

namespace FileService.VideoProcessing;

public class VideoProcessingService : IVideoProcessingService
{
    private readonly ILogger<VideoProcessingService> _logger;
    private readonly IProcessingPipeline _processingPipeline;

    public VideoProcessingService(
        ILogger<VideoProcessingService> logger,
        IProcessingPipeline processingPipeline)
    {
        _logger = logger;
        _processingPipeline = processingPipeline;
    }

    public async Task<UnitResult<Error>> ProcessVideoAsync(
        Guid videoAssetId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting video processing for VideoAssetId: {VideoAssetId}", videoAssetId);

        UnitResult<Error> pipelineResult = await _processingPipeline
            .ProcessAllStepsAsync(videoAssetId, cancellationToken);

        return pipelineResult;
    }
}