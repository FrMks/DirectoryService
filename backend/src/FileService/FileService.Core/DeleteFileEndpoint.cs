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
            [FromServices] IMediaRepository mediaRepository,
            [FromServices] IS3Provider storage,
            [FromServices] HybridCache hybridCache,
            [FromServices] IEventPublisher messageBus,
            [FromServices] ITransactionManager transactionManager,
            CancellationToken cancellationToken) =>
        {
            MediaAsset? mediaAsset = await mediaRepository.GetByIdAsync(mediaAssetId, cancellationToken);
            if (mediaAsset is null)
            {
                return new ErrorsResult(Error.NotFound("media.not.found", "Media asset was not found"));
            }

            if (mediaAsset.Status == MediaStatus.DELETED)
                return Results.Ok(Envelope.Ok());

            if (mediaAsset.Status != MediaStatus.READY)
            {
                return new ErrorsResult(Error.Validation(
                    "media.invalid.status",
                    $"Cannot delete media asset in status {mediaAsset.Status}"));
            }

            StorageKey keyToDelete = mediaAsset.UploadedObject?.Key ?? mediaAsset.RawKey;

            Result<string, Error> deletedFileResult = await storage
                .DeleteFileAsync(keyToDelete, cancellationToken);
            if (deletedFileResult.IsFailure)
            {
                return new ErrorsResult(deletedFileResult.Error);
            }

            UnitResult<Error> markDeletedResult = mediaAsset
                .MarkDeleted(DateTime.UtcNow);
            if (markDeletedResult.IsFailure)
            {
                return new ErrorsResult(markDeletedResult.Error);
            }

            FileDeleted fileDeletedEvent = new(
                EventId: Guid.NewGuid(),
                FileId: mediaAsset.Id,
                Status: mediaAsset.Status.ToString(),
                TargetEntityType: mediaAsset.Owner.Context,
                TargetEntityId: mediaAsset.Owner.EntityId,
                OccurredAt: DateTimeOffset.UtcNow);

            Result<ITransactionScope, Error> transactionResult = await transactionManager.BeginTransaction(cancellationToken);
            if (transactionResult.IsFailure)
            {
                return new ErrorsResult(transactionResult.Error.ToErrors());
            }
            using ITransactionScope transactionScope = transactionResult.Value;

            await messageBus.PublishAsync(fileDeletedEvent);

            await mediaRepository.UpdateAsync(mediaAsset, cancellationToken);

            UnitResult<Error> commitResult = transactionScope.Commit();
            if (commitResult.IsFailure)
            {
                return new ErrorsResult(commitResult.Error.ToErrors());
            }

            await hybridCache.RemoveAsync(
                $"file-service:download-url:{mediaAssetId}",
                cancellationToken);

            return Results.Ok(Envelope.Ok(new { key = deletedFileResult.Value }));
        });

        return endpoints;
    }
}
