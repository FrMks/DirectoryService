using Amazon.S3;
using Amazon.S3.Model;
using FileService.Core.Files;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.S3;

public class S3Provider : IFileStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Options _s3Options;

    public S3Provider(IAmazonS3 s3Client, IOptions<S3Options> s3Options)
    {
        _s3Client = s3Client;
        _s3Options = s3Options.Value;
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
}
