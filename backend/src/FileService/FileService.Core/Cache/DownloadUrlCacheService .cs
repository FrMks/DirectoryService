using CSharpFunctionalExtensions;
using FileService.Core.Files;
using FileService.Domain.Entities.MediaAssetEntity;
using FileService.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;

namespace FileService.Core.Cache;

public sealed class DowloadUrlCacheService
{
    private readonly IS3Provider _s3Provider;
    private readonly HybridCache _hybridCache;
    private readonly IOptions<DownloadUrlCacheOptions> _downloadUrlCacheOptions;
    private readonly ILogger<DowloadUrlCacheService> _logger;

    public DowloadUrlCacheService(
        IS3Provider s3Provider,
        HybridCache hybridCache,
        IOptions<DownloadUrlCacheOptions> downloadUrlCacheOptions,
        ILogger<DowloadUrlCacheService> logger)
    {
        _s3Provider = s3Provider;
        _hybridCache = hybridCache;
        _downloadUrlCacheOptions = downloadUrlCacheOptions;
        _logger = logger;
    }

    public async Task<Result<string, Error>> GetDownloadUrlFromCache(
        MediaAsset mediaAsset,
        CancellationToken cancellationToken)
    {
        if (mediaAsset.UploadedObject == null)
        {
            return Error.Validation(
                "media.invalid.uploaded.object",
                "Uploaded object is null when we try get content url");
        }

        string cacheKey = $"file-service:download-url:{mediaAsset.Id}";
        StorageKey storageKey = mediaAsset.UploadedObject.Key;

        var cacheOptions = new HybridCacheEntryOptions
        {
            LocalCacheExpiration = TimeSpan.FromMinutes(_downloadUrlCacheOptions.Value.ExpirationMinutes),
            Expiration = TimeSpan.FromMinutes(_downloadUrlCacheOptions.Value.ExpirationMinutes),
        };

        try
        {
            string downloadUrl = await _hybridCache.GetOrCreateAsync(
                cacheKey,
                async cancellationToken =>
                {
                    Result<string, Error> downloadUrlResult = await _s3Provider
                        .GenerateDownloadUrlAsync(storageKey);
                    if (downloadUrlResult.IsFailure)
                    {
                        _logger.LogError(
                            "Failed to get download url for media asset {MediaAssetId}: {Error}",
                            mediaAsset.Id,
                            downloadUrlResult.Error);
                        throw new InvalidOperationException(downloadUrlResult.Error.Message);
                    }

                    return downloadUrlResult.Value;
                },
                cacheOptions,
                cancellationToken: cancellationToken);

            return Result.Success<string, Error>(downloadUrl);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError(
                exception,
                "Failed to generate download URL for media asset {MediaAssetId}",
                mediaAsset.Id);

            return Error.Failure(
                "media.download.url.generation.failed",
                exception.Message);
        }
    }
}