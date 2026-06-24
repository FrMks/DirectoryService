using CSharpFunctionalExtensions;
using FileService.Core.Files;
using FileService.Domain.Entities.MediaAssetEntity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Framework.EndpointResults;

namespace FileService.Core;

public static class CancelPendingUpload
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/files/{mediaAssetId:guid}/cancel", async Task<Microsoft.AspNetCore.Http.IResult> (
            [FromRoute] Guid mediaAssetId,
            [FromServices] CancelPendingUploadHandler handler,
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

public sealed class CancelPendingUploadHandler
{
    private readonly ILogger<CancelPendingUploadHandler> _logger;
    private readonly IS3Provider _s3Provider;
    private readonly IMediaRepository _mediaRepository;

    public CancelPendingUploadHandler(
        ILogger<CancelPendingUploadHandler> logger,
        IS3Provider s3Provider,
        IMediaRepository mediaRepository)
    {
        _logger = logger;
        _s3Provider = s3Provider;
        _mediaRepository = mediaRepository;
    }

    public async Task<UnitResult<Error>> Handle(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        Result<MediaAsset, Error> mediaAssetResult = await _mediaRepository
            .GetBy(x => x.Id == mediaAssetId, cancellationToken);
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error;

        MediaAsset mediaAsset = mediaAssetResult.Value;

        if (mediaAsset.Status == Domain.Enums.MediaStatus.DELETED)
            return Result.Success<Error>();

        if (mediaAsset.Status != Domain.Enums.MediaStatus.UPLOADING)
        {
            return Error.Validation(
                "media.asset.status.problem",
                "Media asset should be uploading for do cancel");
        }

        Result<string, Error> rawKeyResult = await _s3Provider
            .DeleteFileAsync(mediaAsset.RawKey, cancellationToken);
        if (rawKeyResult.IsFailure)
            return rawKeyResult.Error;

        UnitResult<Error> markDeletedResult = mediaAsset.MarkDeleted(DateTime.UtcNow);
        if (markDeletedResult.IsFailure)
            return markDeletedResult.Error;

        await _mediaRepository.UpdateAsync(mediaAsset, cancellationToken);

        return UnitResult.Success<Error>();
    }
}
