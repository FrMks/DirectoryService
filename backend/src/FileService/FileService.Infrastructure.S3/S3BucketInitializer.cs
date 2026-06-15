using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.S3;

public interface IS3BucketInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class S3BucketInitializer : IS3BucketInitializer
{
    private readonly S3Options _s3Options;
    private readonly IAmazonS3 _s3Client;
    private ILogger<S3BucketInitializer> _logger;

    public S3BucketInitializer(
        IOptions<S3Options> s3Options,
        IAmazonS3 s3Client,
        ILogger<S3BucketInitializer> logger)
    {
        _s3Options = s3Options.Value;
        _s3Client = s3Client;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("S3 bucket initialization service started");

            if (_s3Options.RequiredBuckets.Count == 0)
            {
                _logger.LogInformation("S3 bucket initialization service required buckets");
                throw new ArgumentException($"{nameof(_s3Options.RequiredBuckets)} is required");
            }

            _logger.LogInformation(
                "Stating S3 buckets initialization. Required buckets: {Buckets}",
                string.Join(", ", _s3Options.RequiredBuckets));

            Task[] tasks = _s3Options.RequiredBuckets
                .Select(bucketName => InitializeBucketAsync(bucketName, cancellationToken))
                .ToArray();

            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("S3 bucket initializaiton service was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error during S3 bucket initialization");
            throw;
        }
    }

    private async Task InitializeBucketAsync(string bucketName, CancellationToken cancellationToken)
    {
        try
        {
            bool bucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);
            if (bucketExists)
            {
                _logger.LogInformation("Bucket {Bucket} already exists", bucketName);
            }
            else
            {
                _logger.LogInformation("Creating bucket '{BucketName}'", bucketName);

                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = bucketName,
                };

                await _s3Client.PutBucketAsync(putBucketRequest, cancellationToken);
            }

            string policy = $$"""
                               {
                                 "Version": "2012-10-17",
                                     "Statement": [
                                         {
                                             "Effect": "Allow",
                                             "Principal": {
                                                 "AWS": ["*"]
                                             },
                                             "Action": ["s3:GetObject"],
                                             "Resource": ["arn:aws:s3:::{{bucketName}}/*"]
                                         }
                                     ]    
                               }
                               """;

            var putPolicyRequest = new PutBucketPolicyRequest
            {
                BucketName = bucketName,
                Policy = policy,
            };

            await _s3Client.PutBucketPolicyAsync(putPolicyRequest, cancellationToken);

            _logger.LogInformation("Bucket {Bucket} set to {Policy}.", bucketName, putPolicyRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize bucket '{BucketName}'", bucketName);
            throw;
        }
    }
}
