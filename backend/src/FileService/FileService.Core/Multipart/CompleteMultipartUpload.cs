using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Core.Processing;
using FileService.Domain.Entities.MediaAssetEntity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Quartz;
using Shared;
using Shared.Core.Database;
using Shared.Framework.EndpointResults;

namespace FileService.Core.Multipart;

public static class CompleteMultipartUpload
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/files/complete-upload", async Task<Microsoft.AspNetCore.Http.IResult> (
            [FromBody] CompleteMultipartUploadRequest request,
            [FromServices] CompleteMultipartUploadHandler handler,
            CancellationToken cancellationToken) =>
        {
            UnitResult<Error> result = await handler.Handle(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(Envelope.Ok())
                : new ErrorsResult(result.Error);
        });

        return endpoints;
    }
}

public sealed class CompleteMultipartUploadHandler
{
    private readonly ILogger<CompleteMultipartUploadHandler> _logger;
    private readonly IS3Provider _s3Provider;
    private readonly IMediaRepository _mediaRepository;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IEnumerable<IProcessingJobFactory> _processingJobFactories;
    private readonly ITransactionManager _transactionManager;

    public CompleteMultipartUploadHandler(
        ILogger<CompleteMultipartUploadHandler> logger,
        IS3Provider s3Provider,
        IMediaRepository mediaRepository,
        ISchedulerFactory schedulerFactory,
        IEnumerable<IProcessingJobFactory> processingJobFactories,
        ITransactionManager transactionManager)
    {
        _logger = logger;
        _s3Provider = s3Provider;
        _mediaRepository = mediaRepository;
        _schedulerFactory = schedulerFactory;
        _processingJobFactories = processingJobFactories;
        _transactionManager = transactionManager;
    }

    public async Task<UnitResult<Error>> Handle(CompleteMultipartUploadRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Completing multipart upload {UploadId} for media asset {MediaAssetId}",
            request.UploadId,
            request.MediaAssetId);

        Result<MediaAsset, Error> mediaAssetResult = await _mediaRepository
            .GetBy(m => m.Id == request.MediaAssetId, cancellationToken);
        if (mediaAssetResult.IsFailure)
        {
            _logger.LogWarning(
                "Media asset {MediaAssetId} was not found while completing multipart upload {UploadId}",
                request.MediaAssetId,
                request.UploadId);

            return mediaAssetResult.Error;
        }

        MediaAsset mediaAsset = mediaAssetResult.Value;

        if (mediaAsset.Status != Domain.Enums.MediaStatus.UPLOADING)
        {
            return Error.Validation(
                "media.asset.status.invalid",
                $"Cannot complete multipart upload for media asset in status {mediaAsset.Status}");
        }

        if (mediaAsset.MediaData.ExpectedChunksCount != request.PartETags.Count)
        {
            _logger.LogWarning(
                "Multipart upload {UploadId} for media asset {MediaAssetId} has invalid part count. Expected {ExpectedChunksCount}, got {ActualChunksCount}",
                request.UploadId,
                mediaAsset.Id,
                mediaAsset.MediaData.ExpectedChunksCount,
                request.PartETags.Count);

            return Error.Validation(
                "multipart.parts.count.invalid",
                "The number of ETags does not match the number of chunks");
        }

        Result<string, Error> completeResult = await _s3Provider.CompleteMultipartUploadAsync(
            mediaAsset.UploadedKey,
            request.UploadId,
            request.PartETags,
            cancellationToken);
        if (completeResult.IsFailure)
        {
            _logger.LogError(
                "Failed to complete multipart upload {UploadId} for media asset {MediaAssetId}: {ErrorMessage}",
                request.UploadId,
                mediaAsset.Id,
                completeResult.Error.Message);

            return completeResult.Error;
        }

        IProcessingJobFactory? processingJobFactory = null;
        if (mediaAsset.RequiresProcessing())
        {
            processingJobFactory = _processingJobFactories.FirstOrDefault(f => f.CanProcess(mediaAsset));
            if (processingJobFactory is null)
            {
                _logger.LogError("No processing job factory found for MediaAssetId: {MediaAssetId}", mediaAsset.Id);
                return Error.Failure("processing.job.not.found", "No processing job factory found");
            }
        }

        Result<ITransactionScope, Error> transactionResult = await _transactionManager
            .BeginTransaction(cancellationToken);
        if (transactionResult.IsFailure)
        {
            _logger.LogError(
                "Failed to begin transaction for MediaAssetId: {MediaAssetId}: {ErrorMessage}",
                mediaAsset.Id,
                transactionResult.Error.Message);
            return transactionResult.Error;
        }

        using ITransactionScope transactionScope = transactionResult.Value;

        try
        {
            UnitResult<Error> markUploadedResult = mediaAsset.MarkUploaded(DateTime.UtcNow);
            if (markUploadedResult.IsFailure)
            {
                transactionScope.Rollback();
                return markUploadedResult.Error;
            }

            if (processingJobFactory is null)
            {
                UnitResult<Error> markReadyResult = mediaAsset
                    .MarkReady(mediaAsset.UploadedKey, DateTime.UtcNow);
                if (markReadyResult.IsFailure)
                {
                    transactionScope.Rollback();
                    return markReadyResult.Error;
                }
            }

            UnitResult<Error> saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
            if (saveResult.IsFailure)
            {
                transactionScope.Rollback();
                _logger.LogError(
                    "Failed to save media asset {MediaAssetId} after completing multipart upload",
                    mediaAsset.Id);
                return saveResult.Error;
            }

            UnitResult<Error> commitResult = transactionScope.Commit();
            if (commitResult.IsFailure)
            {
                _logger.LogError(
                    "Failed to commit media asset {MediaAssetId} after completing multipart upload",
                    mediaAsset.Id);
                return commitResult.Error;
            }
        }
        catch (Exception ex)
        {
            transactionScope.Rollback();
            _logger.LogError(
                ex,
                "Unexpected error while saving media asset {MediaAssetId} after completing multipart upload",
                mediaAsset.Id);
            return Error.Failure(
                "multipart.completion.failed",
                "Failed to save completed multipart upload");
        }

        _logger.LogInformation(
            "Completed multipart upload {UploadId} for media asset {MediaAssetId}",
            request.UploadId,
            mediaAsset.Id);

        if (processingJobFactory is not null)
        {
            try
            {
                IScheduler scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

                IJobDetail job = processingJobFactory.CreateJob(mediaAsset);
                ITrigger trigger = processingJobFactory.CreateTrigger(mediaAsset);

                await scheduler.ScheduleJob(job, trigger, cancellationToken);

                _logger.LogInformation("Scheduled processing job for MediaAssetId: {MediaAssetId}", mediaAsset.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to schedule processing job for MediaAssetId: {MediaAssetId}",
                    mediaAsset.Id);
                return Error.Failure(
                    "processing.job.schedule.failed",
                    "Media upload completed, but processing could not be scheduled");
            }

        }

        return Result.Success<Error>();
    }
}
