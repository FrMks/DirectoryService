using CSharpFunctionalExtensions;
using FileService.Contracts.IntegrationEvents;
using FileService.Core.Event;
using FileService.Core.Multipart;
using FileService.Domain.Entities.MediaAssetEntity;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Hybrid;
using Shared;
using Shared.Core.Database;
using Shared.Framework.EndpointResults;

namespace FileService.Core;

public static class DeleteFileEndpoint
{
    public static IEndpointRouteBuilder MapDeleteFileEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/files/{mediaAssetId:guid}/delete", async Task<Microsoft.AspNetCore.Http.IResult> (
            [FromRoute] Guid mediaAssetId,
            [FromServices] DeleteFileHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result<string, Error> result = await handler.Handle(mediaAssetId, cancellationToken);
            return result.IsSuccess
                ? Results.Ok(Envelope.Ok(new { key = result.Value }))
                : new ErrorsResult(result.Error);
        });

        return endpoints;
    }
}

public sealed class DeleteFileHandler
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IS3Provider _storage;
    private readonly HybridCache _hybridCache;
    private readonly IEventPublisher _eventPublisher;
    private readonly ITransactionManager _transactionManager;

    public DeleteFileHandler(
        IMediaRepository mediaRepository,
        IS3Provider storage,
        HybridCache hybridCache,
        IEventPublisher eventPublisher,
        ITransactionManager transactionManager)
    {
        _mediaRepository = mediaRepository;
        _storage = storage;
        _hybridCache = hybridCache;
        _eventPublisher = eventPublisher;
        _transactionManager = transactionManager;
    }

    public async Task<Result<string, Error>> Handle(
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        MediaAsset? mediaAsset = await _mediaRepository
            .GetByIdAsync(mediaAssetId, cancellationToken);
        if (mediaAsset is null)
            return Error.NotFound("media.not.found", "Media asset was not found");

        if (mediaAsset.Status == MediaStatus.DELETED)
            return string.Empty;

        if (mediaAsset.Status != MediaStatus.READY)
        {
            return Error.Validation(
                "media.invalid.status",
                $"Cannot delete media asset in status {mediaAsset.Status}");
        }

        StorageKey keyToDelete = mediaAsset.UploadedObject?.Key ?? mediaAsset.RawKey;
        Result<string, Error> deletedFileResult = await _storage
            .DeleteFileAsync(keyToDelete, cancellationToken);
        if (deletedFileResult.IsFailure)
            return deletedFileResult.Error;

        return await MarkDeletedAndPublishAsync(
            mediaAsset,
            deletedFileResult.Value,
            cancellationToken);
    }

    private async Task<Result<string, Error>> MarkDeletedAndPublishAsync(
        MediaAsset mediaAsset,
        string deletedKey,
        CancellationToken cancellationToken)
    {
        UnitResult<Error> markDeletedResult = mediaAsset.MarkDeleted(DateTime.UtcNow);
        if (markDeletedResult.IsFailure)
            return markDeletedResult.Error;

        FileDeleted fileDeletedEvent = new(
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

        await _eventPublisher.PublishAsync(fileDeletedEvent);
        await _mediaRepository.UpdateAsync(mediaAsset, cancellationToken);

        UnitResult<Error> commitResult = transactionScope.Commit();
        if (commitResult.IsFailure)
            return commitResult.Error;

        await _hybridCache.RemoveAsync(
            $"file-service:download-url:{mediaAsset.Id}",
            cancellationToken);

        return deletedKey;
    }
}
