using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Core.Files;
using FileService.Domain.Entities.MediaAssetEntity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Framework.EndpointResults;

namespace FileService.Core;

// Дай только ссылку
public static class GetContentUrl
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/files/{mediaAssetId:guid}/content-url", async Task<EndpointResult<GetContentUrlResponse>> (
            [FromRoute] Guid mediaAssetId,
            [FromServices] GetContentUrlHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result<GetContentUrlResponse, Error> result = await handler.Handle(mediaAssetId, cancellationToken);

            return result;
        });

        return endpoints;
    }
}

public sealed class GetContentUrlHandler
{
    private readonly ILogger<GetContentUrlHandler> _logger;
    private readonly IS3Provider _s3Provider;
    private readonly IMediaRepository _mediaRepository;
    private readonly HybridCache _hybridCache;
    private readonly IOptions<DownloadUrlCacheOptions> _downloadUrlCacheOptions;

    public GetContentUrlHandler(
        ILogger<GetContentUrlHandler> logger,
        IS3Provider s3Provider,
        IMediaRepository mediaRepository,
        HybridCache hybridCache,
        IOptions<DownloadUrlCacheOptions> downloadUrlCacheOptions)
    {
        _logger = logger;
        _s3Provider = s3Provider;
        _mediaRepository = mediaRepository;
        _hybridCache = hybridCache;
        _downloadUrlCacheOptions = downloadUrlCacheOptions;
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

        Result<string, Error> downloadUrlResult = await GetDownloadUrlFromCache(mediaAsset, cancellationToken);
        if (downloadUrlResult.IsFailure)
        {
            return downloadUrlResult.Error;
        }

        return new GetContentUrlResponse(
            mediaAsset.Id,
            downloadUrlResult.Value,
            "GET",
            DateTimeOffset.UtcNow.AddMinutes(60));
    }

    private async Task<Result<string, Error>> GetDownloadUrlFromCache(MediaAsset mediaAsset, CancellationToken cancellationToken)
    {
        string cacheKey = $"file-service:download-url:{mediaAsset.Id}";

        var cacheOptions = new HybridCacheEntryOptions
        {
            LocalCacheExpiration = TimeSpan.FromMinutes(_downloadUrlCacheOptions.Value.ExpirationMinutes),
            Expiration = TimeSpan.FromMinutes(_downloadUrlCacheOptions.Value.ExpirationMinutes),
        };

        string downloadUrl;

        try
        {
            downloadUrl = await _hybridCache.GetOrCreateAsync(
                cacheKey,
                async cancellationToken =>
                {
                    Result<string, Error> downloadUrlResult = await _s3Provider.GenerateDownloadUrlAsync(mediaAsset.UploadedObject.Key);
                    if (downloadUrlResult.IsFailure)
                        throw new InvalidOperationException(downloadUrlResult.Error.Message);

                    return downloadUrlResult.Value;
                },
                cacheOptions,
                cancellationToken: cancellationToken);

            return Result.Success<string, Error>(downloadUrl);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError(exception, "Failed to generate download URL for media asset {MediaAssetId}", mediaAsset.Id);

            return Error.Failure(
                "media.download.url.generation.failed",
                exception.Message);
        }
    }
}