using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Core.Files;
using FileService.Domain.Entities.MediaAssetEntity;
using FileService.Domain.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared;
using StackExchange.Redis;

namespace FileService.Core.UploadAndCompleteOnlyOneUrl;

public static class CompleteUpload
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/files/{mediaAssetId:guid}/complete", async Task<Microsoft.AspNetCore.Http.IResult> (
            [FromRoute] Guid mediaAssetId,
            [FromServices] CompleteUploadHandler handler,
            CancellationToken cancellationToken) =>
        {
            UnitResult<Error> result = await handler.Handle(mediaAssetId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok()
                : Results.BadRequest(result.Error);
        });

        return endpoints;
    }
}

public sealed class CompleteUploadHandler
{
    private readonly ILogger<CompleteUploadHandler> _logger;
    private readonly IS3Provider _s3Provider;
    private readonly IMediaRepository _mediaRepository;

    public CompleteUploadHandler(
        ILogger<CompleteUploadHandler> logger,
        IS3Provider s3Provider,
        IMediaRepository mediaRepository)
    {
        _logger = logger;
        _s3Provider = s3Provider;
        _mediaRepository = mediaRepository;
    }

    // Find asset => check status => read storage metadata => compare metadata 
    // => mark ready => save => return success
    public async Task<UnitResult<Error>> Handle(
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        Result<MediaAsset, Error> mediaAssetResult = await _mediaRepository
            .GetBy(m => m.Id == mediaAssetId, cancellationToken);
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error;

        MediaAsset mediaAsset = mediaAssetResult.Value;

        if (mediaAsset.Status == Domain.Enums.MediaStatus.READY)
            return UnitResult.Success<Error>();

        if (mediaAsset.Status != Domain.Enums.MediaStatus.UPLOADING)
        {
            return Error.Validation(
                "media.invalid.status",
                $"Cannot complete upload for media asset in status {mediaAsset.Status}");
        }

        Result<StorageObjectMetadata, Error> metadataResult =
            await _s3Provider.GetMetadataAsync(mediaAsset.RawKey, cancellationToken);
        if (metadataResult.IsFailure)
            return metadataResult.Error;

        StorageObjectMetadata metadata = metadataResult.Value;

        if (metadata.SizeBytes != mediaAsset.MediaData.Size)
        {
            return Error.Validation(
                "media.size.mismatch",
                "Uploaded object size does not match expected size");
        }

        if (!string.Equals(
                metadata.ContentType,
                mediaAsset.MediaData.ContentType.Value,
                StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation(
                "media.content-type.mismatch",
                "Uploaded object content type does not match expected content type");
        }

        Result<StorageReference, Error> storageReferenceResult = StorageReference.Create(
            mediaAsset.RawKey,
            metadata.SizeBytes,
            metadata.ContentType,
            metadata.ETag);
        if (storageReferenceResult.IsFailure)
            return storageReferenceResult.Error;

        UnitResult<Error> attachUploadedObjectResult = mediaAsset
            .AttachUploadedObject(storageReferenceResult.Value);
        if (attachUploadedObjectResult.IsFailure)
            return attachUploadedObjectResult.Error;

        UnitResult<Error> markUploadedResult = mediaAsset.MarkUploaded(DateTime.UtcNow);
        if (markUploadedResult.IsFailure)
            return markUploadedResult.Error;

        UnitResult<Error> markReadyResult = mediaAsset.MarkReady(
            mediaAsset.RawKey,
            DateTime.UtcNow);
        if (markReadyResult.IsFailure)
            return markReadyResult.Error;

        await _mediaRepository.UpdateAsync(mediaAsset, cancellationToken);

        return UnitResult.Success<Error>();
    }
}