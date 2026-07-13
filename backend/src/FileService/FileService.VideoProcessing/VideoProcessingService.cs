using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Shared;

namespace FileService.VideoProcessing;

public class VideoProcessingService
{
    private readonly ILogger<VideoProcessingService> _logger;

    public VideoProcessingService(ILogger<VideoProcessingService> logger)
    {
        _logger = logger;
    }

    public async Task<UnitResult<Error>> ProcessVideoAsync(
        Guid videoAssetId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting video processing for VideoAssetId: {VideoAssetId}", videoAssetId);

        // вызов пайплайна обработки видео

        return UnitResult.Success<Error>();
    }
}