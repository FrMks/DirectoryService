using CSharpFunctionalExtensions;
using FileService.Core;
using FileService.Core.Processing;
using FileService.Domain.Entities;
using FileService.Domain.Errors;
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

        try
        {
            using CancellationTokenSource timeoutCts = CancellationTokenSource
                .CreateLinkedTokenSource(context.CancellationToken);

            timeoutCts.CancelAfter(TimeSpan.FromMinutes(_options.ProcessingTimeoutMinutes));

            UnitResult<Error> result = await _videoProcessingService
                .ProcessVideoAsync(videoAssetId, timeoutCts.Token);
            if (result.IsSuccess)
                return;

            await HandleFailureAsync(videoAssetId, result.Error, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled error while processing video {VideoAssetId}",
                videoAssetId);

            await HandleFailureAsync(videoAssetId, MapException(ex), CancellationToken.None);
        }
    }

    private DateTime CalculateNextRetryTime(VideoProcess videoProcess)
    {
        return DateTime.UtcNow.AddSeconds(_options.RetryDelaySeconds * Math.Pow(2, videoProcess.RetryCount));
    }

    private async Task HandleFailureAsync(
        Guid videoAssetId,
        Error error,
        CancellationToken cancellationToken)
    {
        Result<VideoProcess, Error> processResult =
            await _videoProcessingRepository.GetBy(
                process => process.VideoAssetId == videoAssetId,
                cancellationToken);
        if (processResult.IsFailure)
        {
            _logger.LogError(
                "Could not load VideoProcess for {VideoAssetId}: {Error}",
                videoAssetId,
                processResult.Error);

            return;
        }

        VideoProcess process = processResult.Value;
        ProcessingErrorKind errorKind = _processingErrorClassifier.Classify(error);

        if (process.Status != ProcessingStatus.FAILED)
        {
            UnitResult<Error> failResult = process.Fail(error.Message);
            if (failResult.IsFailure)
            {
                _logger.LogError(
                    "Could not mark VideoProcess as failed for {VideoAssetId}: {Error}",
                    videoAssetId,
                    failResult.Error);
                return;
            }
        }

        if (errorKind == ProcessingErrorKind.Transient &&
            process.CanRetry())
        {
            DateTime nextRetryAt = CalculateNextRetryTime(process);

            UnitResult<Error> scheduleRetryResult = process.ScheduleRetry(nextRetryAt);
            if (scheduleRetryResult.IsFailure)
                return;

            UnitResult<Error> resetResult = process.Reset();
            if (resetResult.IsFailure)
                return;

            Result<VideoAsset, Error> retryAssetResult = await _mediaRepository
                .GetVideoAssetBy(asset => asset.Id == videoAssetId, cancellationToken);
            if (retryAssetResult.IsFailure)
                return;

            UnitResult<Error> resetAssetResult = retryAssetResult.Value
                .ResetForRetry(DateTime.UtcNow);
            if (resetAssetResult.IsFailure)
                return;

            UnitResult<Error> saveResult = await _transactionManager
                .SaveChangesAsync(cancellationToken);
            if (saveResult.IsFailure)
                return;

            try
            {
                await _processingRetryScheduler.ScheduleAsync(
                    videoAssetId,
                    process.RetryCount,
                    nextRetryAt,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Could not create retry trigger for {VideoAssetId}",
                    videoAssetId);

                process.Fail("Could not schedule video processing retry");
                retryAssetResult.Value.FailProcessing(DateTime.UtcNow);
                await _transactionManager.SaveChangesAsync(CancellationToken.None);
            }

            return;
        }

        Result<VideoAsset, Error> assetResult =
            await _mediaRepository.GetVideoAssetBy(
                asset => asset.Id == videoAssetId,
                cancellationToken);
        if (assetResult.IsSuccess)
            assetResult.Value.FailProcessing(DateTime.UtcNow);

        await _transactionManager.SaveChangesAsync(cancellationToken);
    }

    private static Error MapException(Exception exception)
    {
        return exception switch
        {
            OperationCanceledException => FileError.OperationCancelled(),
            TimeoutException => FileError.NetworkIssue(),
            _ => Error.Failure("processing.job.exception", exception.Message),
        };
    }
}
