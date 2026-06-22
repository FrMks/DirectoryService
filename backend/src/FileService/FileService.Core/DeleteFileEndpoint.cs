using CSharpFunctionalExtensions;
using FileService.Core.Files;
using FileService.Domain.Entities.MediaAssetEntity;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared;
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

            await mediaRepository.UpdateAsync(mediaAsset, cancellationToken);

            return Results.Ok(Envelope.Ok(new { key = deletedFileResult.Value }));
        });

        return endpoints;
    }
}
