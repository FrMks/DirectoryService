using Amazon.S3;
using Amazon.S3.Model;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Core.Files;
using FileService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;

namespace FileService.Infrastructure.S3;

public class S3Provider : IS3Provider
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Options _s3Options;
    private readonly ILogger<S3Provider> _logger;

    private readonly SemaphoreSlim _requestsSemaphore;

    public S3Provider(
        IAmazonS3 s3Client,
        IOptions<S3Options> s3Options,
        ILogger<S3Provider> logger)
    {
        _s3Client = s3Client;
        _s3Options = s3Options.Value;
        _logger = logger;

        _requestsSemaphore = new SemaphoreSlim(_s3Options.MaxConcurrentRequests);
    }

    public async Task<Result<string, Error>> StartMultipartUploadAsync(
        StorageKey storageKey,
        MediaData mediaData,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new InitiateMultipartUploadRequest()
            {
                BucketName = storageKey.Bucket,
                Key = storageKey.Value,
                ContentType = mediaData.ContentType.Value,
            };

            InitiateMultipartUploadResponse response = await _s3Client.InitiateMultipartUploadAsync(request, cancellationToken);
            return Result.Success<string, Error>(response.UploadId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting multipart upload for bucket {BucketName} and key {Key}", storageKey.Bucket, storageKey.Value);
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<IReadOnlyList<ChunkUploadUrl>, Error>> GenerateAllChunkUploadUrlsAsync(
        StorageKey storageKey,
        string uploadId,
        int totalChunks,
        CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<Task<ChunkUploadUrl>> tasks = Enumerable.Range(1, totalChunks)
                .Select(async partNumber =>
                {
                    await _requestsSemaphore.WaitAsync(cancellationToken);

                    try
                    {
                        var request = new GetPreSignedUrlRequest
                        {
                            BucketName = storageKey.Bucket,
                            Key = storageKey.Value,
                            Verb = HttpVerb.PUT,
                            UploadId = uploadId,
                            PartNumber = partNumber,
                            Expires = DateTime.UtcNow.AddMinutes(_s3Options.UploadUrlExpirationMinutes),
                            Protocol = _s3Options.WithSsl ? Protocol.HTTPS : Protocol.HTTP,
                        };

                        string? url = await _s3Client.GetPreSignedURLAsync(request);

                        return new ChunkUploadUrl(partNumber, url);
                    }
                    finally
                    {
                        _requestsSemaphore.Release();
                    }
                });

            ChunkUploadUrl[] results = await Task.WhenAll(tasks);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating multipart upload urls for bucket {BucketName} and key {Key}", storageKey.Bucket, storageKey.Value);
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<UnitResult<Error>> UploadFileAsync(
        StorageKey storageKey,
        Stream stream,
        MediaData mediaData,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = storageKey.Bucket,
                Key = storageKey.Value,
                InputStream = stream,
                ContentType = mediaData.ContentType.Value,
            };

            await _s3Client.PutObjectAsync(request, cancellationToken);

            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to bucket {BucketName} and key {Key}", storageKey.Bucket, storageKey.Value);
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string, Error>> DownloadFileAsync(
        StorageKey storageKey,
        string tempPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = storageKey.Bucket,
                Key = storageKey.Value,
            };

            using GetObjectResponse response = await _s3Client.GetObjectAsync(request, cancellationToken);
            await using var fileStream = File.Create(tempPath);
            await response.ResponseStream.CopyToAsync(fileStream, cancellationToken);

            return tempPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file from bucket {BucketName} and key {Key}", storageKey.Bucket, storageKey.Value);
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string, Error>> DeleteFileAsync(
        StorageKey storageKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = storageKey.Bucket,
                Key = storageKey.Value,
            };

            await _s3Client.DeleteObjectAsync(request, cancellationToken);

            return storageKey.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from bucket {BucketName} and key {Key}", storageKey.Bucket, storageKey.Value);
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string, Error>> GenerateDownloadUrlAsync(StorageKey storageKey)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = storageKey.Bucket,
                Key = storageKey.Value,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddHours(_s3Options.DownloadUrlExpirationHours),
                Protocol = _s3Options.WithSsl ? Protocol.HTTPS : Protocol.HTTP,
            };

            string? response = await _s3Client.GetPreSignedURLAsync(request);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating download url for bucket {BucketName} and key {Key}", storageKey.Bucket, storageKey.Value);
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string, Error>> GenerateUploadUrlAsync(
        StorageKey storageKey,
        MediaData mediaData,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = storageKey.Bucket,
                Key = storageKey.Value,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(_s3Options.UploadUrlExpirationMinutes),
                Protocol = _s3Options.WithSsl ? Protocol.HTTPS : Protocol.HTTP,
            };

            string response = await _s3Client.GetPreSignedURLAsync(request);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating upload url for bucket {BucketName} and key {Key}", storageKey.Bucket, storageKey.Value);
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string, Error>> CompleteMultipartUploadAsync(
        StorageKey storageKey,
        string uploadId,
        IReadOnlyList<PartETagDto> partETags,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new Amazon.S3.Model.CompleteMultipartUploadRequest
            {
                BucketName = storageKey.Bucket,
                Key = storageKey.Value,
                UploadId = uploadId,
                PartETags = partETags.Select(p => new PartETag(p.PartNumber, p.ETag)).ToList(),
            };

            CompleteMultipartUploadResponse response = await _s3Client
                .CompleteMultipartUploadAsync(request, cancellationToken);

            return response.Key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing multipart upload for bucket {BucketName} and key {Key}", storageKey.Bucket, storageKey.Value);
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<IReadOnlyList<string>, Error>> GenerateDownloadUrlsAsync(IEnumerable<StorageKey> storageKeys)
    {
        var urls = new List<string>();

        foreach (StorageKey storageKey in storageKeys)
        {
            Result<string, Error> urlResult = await GenerateDownloadUrlAsync(storageKey);
            if (urlResult.IsFailure)
                return urlResult.Error;

            urls.Add(urlResult.Value);
        }

        return urls;
    }

    public async Task<Result<StorageObjectMetadata, Error>> GetMetadataAsync(StorageKey storageKey, CancellationToken cancellationToken)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = storageKey.Bucket,
                Key = storageKey.Value,
            };

            GetObjectMetadataResponse response =
                await _s3Client.GetObjectMetadataAsync(request, cancellationToken);

            return new StorageObjectMetadata(
                response.Headers.ContentType,
                response.Headers.ContentLength,
                response.ETag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error for get metadata was include in GetMetadataAsync method.");
            return S3ErrorMapper.ToError(ex);
        }
    }
}