using CSharpFunctionalExtensions;
using FileService.Core.Files;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared;

namespace FileService.Core;

public static class DeleteFileEndpoint
{
    public static IEndpointRouteBuilder MapDeleteFileEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/files/{mediaAssetId:guid}", async Task<Microsoft.AspNetCore.Http.IResult> (
            [FromRoute] Guid mediaAssetId,
            [FromServices] IMediaRepository mediaRepository,
            [FromServices] IS3Provider storage,
            CancellationToken cancellationToken) =>
        {
            var mediaAsset = await mediaRepository.GetByIdAsync(mediaAssetId, cancellationToken);
            if (mediaAsset is null)
            {
                return Results.NotFound(Error.NotFound("media.not.found", "Media asset was not found"));
            }

            UnitResult<Error> markDeletedResult = mediaAsset
                .MarkDeleted(DateTime.UtcNow);
            if (markDeletedResult.IsFailure)
            {
                return Results.BadRequest(markDeletedResult.Error);
            }

            await mediaRepository.UpdateAsync(mediaAsset, cancellationToken);

            Result<string, Error> deletedFileResult = await storage
                .DeleteFileAsync(mediaAsset.RawKey, cancellationToken);
            if (deletedFileResult.IsFailure)
            {
                return Results.BadRequest(deletedFileResult.Error);
            }

            return Results.Ok(new { key = deletedFileResult.Value });
        });

        return endpoints;
    }
}