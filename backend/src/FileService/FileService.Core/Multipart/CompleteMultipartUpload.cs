using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Core.Files;
using FileService.Domain.Entities.MediaAssetEntity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared;

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
                ? Results.Ok()
                : Results.BadRequest(result.Error);
        });

        return endpoints;
    }
}

public sealed class CompleteMultipartUploadHandler
{
    private readonly ILogger<CompleteMultipartUploadHandler> _logger;
    private readonly IS3Provider _s3Provider;
    private readonly IMediaRepository _mediaRepository;

    public CompleteMultipartUploadHandler(
        ILogger<CompleteMultipartUploadHandler> logger,
        IS3Provider s3Provider,
        IMediaRepository mediaRepository)
    {
        _logger = logger;
        _s3Provider = s3Provider;
        _mediaRepository = mediaRepository;
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

        if (mediaAsset.MediaData.ExpectedChunksCount != request.PartETags.Count)
        {
            _logger.LogWarning(
                "Multipart upload {UploadId} for media asset {MediaAssetId} has invalid part count. Expected {ExpectedChunksCount}, got {ActualChunksCount}",
                request.UploadId,
                mediaAsset.Id,
                mediaAsset.MediaData.ExpectedChunksCount,
                request.PartETags.Count);

            return Error.Failure(null, "The number of ETags does not match the number of chunks");
        }

        Result<string, Error> completeResult = await _s3Provider.CompleteMultipartUploadAsync(
            mediaAsset.RawKey,
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

        UnitResult<Error> markUploadedResult = mediaAsset.MarkUploaded(DateTime.UtcNow);
        if (markUploadedResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to mark media asset {MediaAssetId} as uploaded after multipart upload {UploadId}: {ErrorMessage}",
                mediaAsset.Id,
                request.UploadId,
                markUploadedResult.Error.Message);

            return markUploadedResult.Error;
        }

        await _mediaRepository.UpdateAsync(mediaAsset, cancellationToken);

        _logger.LogInformation(
            "Completed multipart upload {UploadId} for media asset {MediaAssetId}",
            request.UploadId,
            mediaAsset.Id);

        return Result.Success<Error>();
    }
}
