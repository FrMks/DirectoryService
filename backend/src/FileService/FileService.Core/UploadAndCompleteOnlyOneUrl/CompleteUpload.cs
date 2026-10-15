using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Contracts.IntegrationEvents;
using FileService.Core.Event;
using FileService.Core.Multipart;
using FileService.Domain.Entities.MediaAssetEntity;
using FileService.Domain.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Core.Database;
using Shared.Framework.EndpointResults;
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
                ? Results.Ok(Envelope.Ok())
                : new ErrorsResult(result.Error);
        });

        return endpoints;
    }
}

public sealed class CompleteUploadHandler
{
    private readonly ILogger<CompleteUploadHandler> _logger;
    private readonly IS3Provider _s3Provider;
    private readonly IMediaRepository _mediaRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IEventPublisher _messageBus;

    public CompleteUploadHandler(
        ILogger<CompleteUploadHandler> logger,
        IS3Provider s3Provider,
        IMediaRepository mediaRepository,
        ITransactionManager transactionManager,
        IEventPublisher messageBus)
    {
        _logger = logger;
        _s3Provider = s3Provider;
        _mediaRepository = mediaRepository;
        _transactionManager = transactionManager;
        _messageBus = messageBus;
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

        // Мы проверяем объект, который клиент загрузил. Значит надо смотреть на uplaod destination.
        Result<StorageObjectMetadata, Error> metadataResult =
            await _s3Provider.GetMetadataAsync(mediaAsset.UploadedKey, cancellationToken);
        if (metadataResult.IsFailure)
        {
            if (metadataResult.Error.Type == ErrorType.NotFound)
            {
                return Error.Validation(
                    "media.upload.not.found",
                    "The file must be uploaded before completing the upload");
            }

            return metadataResult.Error;
        }

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
            mediaAsset.UploadedKey,
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

        return await MarkReadyAndPublishAsync(mediaAsset, cancellationToken);
    }

    private async Task<UnitResult<Error>> MarkReadyAndPublishAsync(
        MediaAsset mediaAsset,
        CancellationToken cancellationToken)
    {
        UnitResult<Error> markReadyResult = mediaAsset.MarkReady(
            mediaAsset.RawKey,
            DateTime.UtcNow);
        if (markReadyResult.IsFailure)
            return markReadyResult.Error;

        FileReady fileReadyEvent = new(
            EventId: Guid.NewGuid(),
            FileId: mediaAsset.Id,
            Status: mediaAsset.Status.ToString(),
            TargetEntityType: mediaAsset.Owner.Context,
            TargetEntityId: mediaAsset.Owner.EntityId,
            OccurredAt: DateTimeOffset.UtcNow);

        Result<ITransactionScope, Error> transactionResult =
            await _transactionManager.BeginTransaction(cancellationToken);
        if (transactionResult.IsFailure)
            return transactionResult.Error;

        using ITransactionScope transactionScope = transactionResult.Value;

        await _messageBus.PublishAsync(fileReadyEvent);
        await _mediaRepository.UpdateAsync(mediaAsset, cancellationToken);

        UnitResult<Error> commitResult = transactionScope.Commit();
        return commitResult.IsSuccess
            ? UnitResult.Success<Error>()
            : commitResult.Error;
    }
}
