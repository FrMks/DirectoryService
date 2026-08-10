using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Quartz;
using Shared;

namespace FileService.VideoProcessing.Jobs;

[DisallowConcurrentExecution]
public class VideoProcessingJob : IJob
{
    private readonly ILogger<VideoProcessingJob> _logger;
    private readonly IVideoProcessingService _videoProcessingService;

    public VideoProcessingJob(
        ILogger<VideoProcessingJob> logger,
        IVideoProcessingService videoProcessingService)
    {
        _logger = logger;
        _videoProcessingService = videoProcessingService;
    }

    public static readonly JobKey VideoAssetIdKey = new("VideoAssetId");

    public async Task Execute(IJobExecutionContext context)
    {
        JobDataMap dataMap = context.MergedJobDataMap;
        Guid videoAssetId = dataMap.GetGuid(VideoAssetIdKey.Name);

        _logger.LogInformation("Stating video processing job for VideoAssetId: {VideoAssetId}", videoAssetId);

        UnitResult<Error> result = await _videoProcessingService
            .ProcessVideoAsync(videoAssetId, context.CancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError(
                "Video processing failed for VideoAssetId: {VideoAssetId}. Error: {Error}",
                videoAssetId,
                result.Error);

            throw new JobExecutionException(refireImmediately: false);
        }
    }
}