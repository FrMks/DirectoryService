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

// Дай только ссылку
public static class GetContentUrl
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/files/{mediaAssetId:guid}/content-url", async Task<Microsoft.AspNetCore.Http.IResult> (
            [FromRoute] Guid mediaAssetId,
            [FromServices] GetContentUrlHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result<GetContentUrlResponse, Error> result = await handler.Handle(mediaAssetId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Error);
        });

        return endpoints;
    }
}

public sealed class GetContentUrlHandler
{
    private readonly ILogger<GetContentUrlHandler> _logger;
    private readonly IS3Provider _s3Provider;
    private readonly IMediaRepository _mediaRepository;

    public GetContentUrlHandler(
        ILogger<GetContentUrlHandler> logger,
        IS3Provider s3Provider,
        IMediaRepository mediaRepository)
    {
        _logger = logger;
        _s3Provider = s3Provider;
        _mediaRepository = mediaRepository;
    }

    public async Task<Result<GetContentUrlResponse, Error>> Handle(
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        Result<MediaAsset, Error> mediaAssetResult = await _mediaRepository
            .GetBy(m => m.Id == mediaAssetId, cancellationToken);
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error;

        MediaAsset mediaAsset = mediaAssetResult.Value;

        if (mediaAsset.Status != Domain.Enums.MediaStatus.READY)
        {
            return Error.Validation(
                "media.invalid.status",
                $"Cannot get uploaded object becouse media asset statis is {mediaAsset.Status}. But it should be {Domain.Enums.MediaStatus.READY}");
        }

        if (mediaAsset.UploadedObject == null)
        {
            return Error.Validation(
                "media.invalid.uploaded.object",
                "Uploaded object is null when we try get content url");
        }

        Result<string, Error> downloadUrlResult = await _s3Provider.GenerateDownloadUrlAsync(mediaAsset.UploadedObject.Key);
        if (downloadUrlResult.IsFailure)
            return downloadUrlResult.Error;

        return new GetContentUrlResponse(
            mediaAsset.Id,
            downloadUrlResult.Value,
            "GET",
            DateTimeOffset.UtcNow.AddHours(24));
    }
}