using CSharpFunctionalExtensions;
using FileService.Core;
using FileService.Core.Processing;
using FileService.Domain.Entities;
using FileService.Domain.MediaProcessing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Shared;
using Shared.Core.Database;

namespace FileService.VideoProcessing.Jobs;

[DisallowConcurrentExecution]
public class VideoProcessingJob : IJob
{
    private readonly ILogger<VideoProcessingJob> _logger;
    private readonly IVideoProcessingService _videoProcessingService;
    private readonly IProcessingErrorClassifier _processingErrorClassifier;
    private readonly IVideoProcessingRepository _videoProcessingRepository;
    private readonly IMediaRepository _mediaRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly VideoProcessingOptions _options;
    private readonly IProcessingRetryScheduler _processingRetryScheduler;

    public VideoProcessingJob(
        ILogger<VideoProcessingJob> logger,
        IVideoProcessingService videoProcessingService,
        IProcessingErrorClassifier processingErrorClassifier,
        IVideoProcessingRepository videoProcessingRepository,
        IMediaRepository mediaRepository,
        ITransactionManager transactionManager,
        IOptions<VideoProcessingOptions> options,
        IProcessingRetryScheduler processingRetryScheduler)
    {
        _logger = logger;
        _videoProcessingService = videoProcessingService;
        _processingErrorClassifier = processingErrorClassifier;
        _videoProcessingRepository = videoProcessingRepository;
        _mediaRepository = mediaRepository;
        _transactionManager = transactionManager;
        _options = options.Value;
        _processingRetryScheduler = processingRetryScheduler;
    }

    public static readonly JobKey VideoAssetIdKey = new("VideoAssetId");

    public async Task Execute(IJobExecutionContext context)
    {
        Guid videoAssetId = context.MergedJobDataMap
            .GetGuid(VideoProcessingJob.VideoAssetIdKey.Name);

        UnitResult<Error> result = await _videoProcessingService
            .ProcessVideoAsync(videoAssetId, context.CancellationToken);
        if (result.IsSuccess)
            return;

        ProcessingErrorKind errorKind =
            _processingErrorClassifier.Classify(result.Error);
        if (errorKind == ProcessingErrorKind.Permanent)
            return;

        Result<VideoProcess, Error> processResult =
            await _videoProcessingRepository.GetBy(
                process => process.VideoAssetId == videoAssetId,
                context.CancellationToken);
        if (processResult.IsFailure)
            return;

        VideoProcess process = processResult.Value;
        if (!process.CanRetry())
            return;

        DateTime nextRetryAt = CalculateNextRetryTime(process);

        UnitResult<Error> scheduleRetryResult = process.ScheduleRetry(nextRetryAt);
        if (scheduleRetryResult.IsFailure)
            return;

        UnitResult<Error> resetResult = process.Reset();
        if (resetResult.IsFailure)
            return;

        Result<VideoAsset, Error> assetResult = await _mediaRepository
            .GetVideoAssetBy(asset => asset.Id == videoAssetId, context.CancellationToken);
        if (assetResult.IsFailure)
            return;

        UnitResult<Error> resetAssetResult = assetResult.Value
            .ResetForRetry(DateTime.UtcNow);
        if (resetAssetResult.IsFailure)
            return;

        UnitResult<Error> transactionResult = await _transactionManager.SaveChangesAsync(
            context.CancellationToken);
        if (transactionResult.IsFailure)
            return;

        await _processingRetryScheduler.ScheduleAsync(
            videoAssetId,
            process.RetryCount,
            nextRetryAt,
            context.CancellationToken);

        return;
    }

    private DateTime CalculateNextRetryTime(VideoProcess videoProcess)
    {
        return DateTime.UtcNow.AddSeconds(_options.RetryDelaySeconds * Math.Pow(2, videoProcess.RetryCount));
    }
}
