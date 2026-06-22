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

namespace FileService.Core;

public static class GetFilesByTargetEntity
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/files", async Task<Microsoft.AspNetCore.Http.IResult> (
            [FromQuery] string context,
            [FromQuery] Guid contextId,
            [FromServices] GetFilesByTargetEntityHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<FileResponse>, Error> result = await handler.Handle(context, contextId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Error);
        });

        return endpoints;
    }
}

public sealed class GetFilesByTargetEntityHandler
{
    private readonly ILogger<GetFilesByTargetEntityHandler> _logger;
    private readonly IS3Provider _s3Provider;
    private readonly IMediaRepository _mediaRepository;

    public GetFilesByTargetEntityHandler(
        ILogger<GetFilesByTargetEntityHandler> logger,
        IS3Provider s3Provider,
        IMediaRepository mediaRepository)
    {
        _logger = logger;
        _s3Provider = s3Provider;
        _mediaRepository = mediaRepository;
    }

    public async Task<Result<IReadOnlyList<FileResponse>, Error>> Handle(
        string context,
        Guid contextId,
        CancellationToken cancellationToken)
    {
        Result<MediaOwner, Error> mediaOwnerResult = MediaOwner.Create(context, contextId);
        if (mediaOwnerResult.IsFailure)
            return mediaOwnerResult.Error;

        MediaOwner mediaOwner = mediaOwnerResult.Value;

        Result<IReadOnlyList<MediaAsset>, Error> mediaAssetsResult = await _mediaRepository
            .GetManyBy(
                x => x.Owner.Context == mediaOwner.Context
                && x.Owner.EntityId == mediaOwner.EntityId
                && x.Status != Domain.Enums.MediaStatus.DELETED,
                cancellationToken);
        if (mediaAssetsResult.IsFailure)
            return mediaAssetsResult.Error;

        IReadOnlyList<MediaAsset> mediaAssets = mediaAssetsResult.Value;

        List<FileResponse> files = [];

        foreach (var mediaAsset in mediaAssets)
        {
            string? contentUrl = null;

            if (mediaAsset.Status == Domain.Enums.MediaStatus.READY
                && mediaAsset.UploadedObject != null)
            {
                Result<string, Error> downloadurlResult =
                    await _s3Provider.GenerateDownloadUrlAsync(mediaAsset.UploadedObject.Key);
                if (downloadurlResult.IsFailure)
                    return downloadurlResult.Error;

                contentUrl = downloadurlResult.Value;
            }

            files.Add(new FileResponse(
                mediaAsset.Id,
                mediaAsset.MediaData.FileName.Name,
                mediaAsset.MediaData.ContentType.Value,
                mediaAsset.MediaData.Size,
                mediaAsset.Status.ToString(),
                mediaAsset.AssetType.ToString(),
                mediaAsset.Owner.Context,
                mediaAsset.Owner.EntityId,
                contentUrl,
                mediaAsset.CreatedAt,
                mediaAsset.UpdatedAt));
        }

        return files;
    }
}