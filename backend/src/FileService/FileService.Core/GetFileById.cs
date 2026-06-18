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

namespace FileService.Core;

// Дай всю карточку файла
public static class GetFileById
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/files/{mediaAssetId:guid}", async Task<Microsoft.AspNetCore.Http.IResult> (
            [FromRoute] Guid mediaAssetId,
            [FromServices] GetFileByIdHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result<FileResponse, Error> result = await handler.Handle(mediaAssetId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Error);
        });

        return endpoints;
    }
}

public sealed class GetFileByIdHandler
{
    private readonly ILogger<GetFileByIdHandler> _logger;
    private readonly IS3Provider _s3Provider;
    private readonly IMediaRepository _mediaRepository;

    public GetFileByIdHandler(
        ILogger<GetFileByIdHandler> logger,
        IS3Provider s3Provider,
        IMediaRepository mediaRepository)
    {
        _logger = logger;
        _s3Provider = s3Provider;
        _mediaRepository = mediaRepository;
    }

    public async Task<Result<FileResponse, Error>> Handle(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        Result<MediaAsset, Error> mediaAssetResult = await _mediaRepository
            .GetBy(m => m.Id == mediaAssetId, cancellationToken);
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error;

        MediaAsset mediaAsset = mediaAssetResult.Value;

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

        return new FileResponse(
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
            mediaAsset.UpdatedAt);
    }
}