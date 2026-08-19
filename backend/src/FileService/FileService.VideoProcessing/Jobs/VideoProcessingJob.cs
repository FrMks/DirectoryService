using CSharpFunctionalExtensions;
using FileService.Core;
using FileService.Core.Outbox;
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
    private readonly IProcessingRetryOutboxRepository _processingRetryOutboxRepository;

    public VideoProcessingJob(
        ILogger<VideoProcessingJob> logger,
        IVideoProcessingService videoProcessingService,
        IProcessingErrorClassifier processingErrorClassifier,
        IVideoProcessingRepository videoProcessingRepository,
        IMediaRepository mediaRepository,
        ITransactionManager transactionManager,
        IOptions<VideoProcessingOptions> options,
        IProcessingRetryOutboxRepository processingRetryOutboxRepository)
    {
        _logger = logger;
        _videoProcessingService = videoProcessingService;
        _processingErrorClassifier = processingErrorClassifier;
        _videoProcessingRepository = videoProcessingRepository;
        _mediaRepository = mediaRepository;
        _transactionManager = transactionManager;
        _options = options.Value;
        _processingRetryOutboxRepository = processingRetryOutboxRepository;
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

    private DateTimeOffset CalculateNextRetryTime(VideoProcess videoProcess)
    {
        return DateTimeOffset.UtcNow.AddSeconds(
            _options.RetryDelaySeconds * Math.Pow(2, videoProcess.RetryCount));
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

        if (errorKind == ProcessingErrorKind.Transient &&
            process.CanRetry())
        {
            Result<VideoAsset, Error> retryAssetResult = await _mediaRepository
                .GetVideoAssetBy(asset => asset.Id == videoAssetId, cancellationToken);
            if (retryAssetResult.IsFailure)
                return;

            Result<ITransactionScope, Error> beginTransactionResult = await _transactionManager
                .BeginTransaction(cancellationToken);
            if (beginTransactionResult.IsFailure)
                return;

            using ITransactionScope transaction = beginTransactionResult.Value;

            if (process.Status != ProcessingStatus.FAILED)
            {
                UnitResult<Error> failResult = process.Fail(error.Message);
                if (failResult.IsFailure)
                {
                    transaction.Rollback();
                    _logger.LogError(
                        "Could not mark VideoProcess as failed for {VideoAssetId}: {Error}",
                        videoAssetId,
                        failResult.Error);
                    return;
                }
            }

            DateTimeOffset nextRetryAt = CalculateNextRetryTime(process);

            UnitResult<Error> scheduleRetryResult = process.ScheduleRetry(nextRetryAt.UtcDateTime);
            if (scheduleRetryResult.IsFailure)
            {
                transaction.Rollback();
                return;
            }

            UnitResult<Error> resetResult = process.Reset();
            if (resetResult.IsFailure)
            {
                transaction.Rollback();
                return;
            }

            UnitResult<Error> resetAssetResult = retryAssetResult.Value
                .ResetForRetry(DateTime.UtcNow);
            if (resetAssetResult.IsFailure)
            {
                transaction.Rollback();
                return;
            }

            UnitResult<Error> createRetryOutboxResult = await _processingRetryOutboxRepository
                .CreateAsync(videoAssetId, process.RetryCount, nextRetryAt, cancellationToken);
            if (createRetryOutboxResult.IsFailure)
            {
                transaction.Rollback();
                _logger.LogError(createRetryOutboxResult.Error.Message);
                return;
            }

            UnitResult<Error> saveResult = await _transactionManager
                .SaveChangesAsync(cancellationToken);
            if (saveResult.IsFailure)
            {
                transaction.Rollback();
                return;
            }

            UnitResult<Error> commitResult = transaction.Commit();
            if (commitResult.IsFailure)
            {
                _logger.LogError(
                    "Could not commit retry state for {VideoAssetId}: {Error}",
                    videoAssetId,
                    commitResult.Error);
            }

            return;
        }

        if (process.Status != ProcessingStatus.FAILED)
        {
            UnitResult<Error> failResult = process.Fail(error.Message);
            if (failResult.IsFailure)
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
