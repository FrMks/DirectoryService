using Amazon.S3;
using Amazon.S3.Model;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Core.Files;
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
        string bucketName,
        string key,
        string contentType,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new InitiateMultipartUploadRequest()
            {
                BucketName = bucketName,
                Key = key,
                ContentType = contentType,
            };

            InitiateMultipartUploadResponse response = await _s3Client.InitiateMultipartUploadAsync(request, cancellationToken);
            return Result.Success<string, Error>(response.UploadId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting multipart upload for bucket {BucketName} and key {Key}", bucketName, key);
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<IReadOnlyList<string>, Error>> GenerateAllChunkUploadUrlsAsync(
        string bucketName,
        string key,
        string uploadId,
        int totalChunks,
        CancellationToken cancellationToken)
    {
        try
        {
            var tasks = Enumerable.Range(1, totalChunks)
                .Select(async partNumber =>
                {
                    await _requestsSemaphore.WaitAsync(cancellationToken);

                    try
                    {
                        var request = new GetPreSignedUrlRequest
                        {
                            BucketName = bucketName,
                            Key = key,
                            Verb = HttpVerb.PUT,
                            UploadId = uploadId,
                            PartNumber = partNumber,
                            Expires = DateTime.UtcNow.AddHours(_s3Options.UploadUrlExpirationHours),
                            Protocol = _s3Options.WithSSL ? Protocol.HTTPS : Protocol.HTTP,
                        };

                        string? url = await _s3Client.GetPreSignedURLAsync(request);

                        return url;
                    }
                    finally
                    {
                        _requestsSemaphore.Release();
                    }
                });

            string[] results = await Task.WhenAll(tasks);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating multipart upload urls for bucket {BucketName} and key {Key}", bucketName, key);
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task UploadFileAsync(
        Stream stream,
        string bucketName,
        string key,
        string contentType,
        CancellationToken cancellationToken)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<Result<string?, Error>> GenerateDownloadUrlAsync(
        string bucketName,
        string key)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = key,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddHours(_s3Options.DownloadUrlExpirationHours),
                Protocol = _s3Options.WithSSL ? Protocol.HTTPS : Protocol.HTTP,
            };

            string? response = await _s3Client.GetPreSignedURLAsync(request);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating download url for bucket {BucketName} and key {Key}", bucketName, key);
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string?, Error>> GenerateUploadUrlAsync(
        string bucketName,
        string key)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = key,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddHours(6),
                Protocol = _s3Options.WithSSL ? Protocol.HTTPS : Protocol.HTTP,
            };

            string? response = await _s3Client.GetPreSignedURLAsync(request);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating upload url for bucket {BucketName} and key {Key}", bucketName, key);
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string, Error>> CompleteMultipartUploadAsync(
        string bucketName,
        string key,
        string uploadId,
        IReadOnlyList<PartETagDto> partETags,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new CompleteMultipartUploadRequest
            {
                BucketName = bucketName,
                Key = key,
                UploadId = uploadId,
                PartETags = partETags.Select(p => new PartETag(p.PartNumber, p.ETag)).ToList(),
            };

            var response = await _s3Client.CompleteMultipartUploadAsync(request, cancellationToken);

            return response.Key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing multipart upload for bucket {BucketName} and key {Key}", bucketName, key);
            return S3ErrorMapper.ToError(ex);
        }
    }
}