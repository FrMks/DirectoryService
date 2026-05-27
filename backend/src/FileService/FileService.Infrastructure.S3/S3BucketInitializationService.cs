using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.S3;

public class S3BucketInitializationService : BackgroundService
{
    private readonly S3Options _s3Options;
    private readonly IAmazonS3 _s3Client;
    private ILogger<S3BucketInitializationService> _logger;

    public S3BucketInitializationService(
        IOptions<S3Options> s3Options,
        IAmazonS3 s3Client,
        ILogger<S3BucketInitializationService> logger)
    {
        _s3Options = s3Options.Value;
        _s3Client = s3Client;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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
                .Select(bucketName => InitializeBucketAsync(bucketName, stoppingToken))
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
                return;
            }

            _logger.LogInformation("Creatin bucket '{BucketName}'", bucketExists);

            var putBucketRequest = new PutBucketRequest
            {
                BucketName = bucketName,
            };

            string policy = $$$"""
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

            await _s3Client.PutBucketAsync(putBucketRequest, cancellationToken);

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