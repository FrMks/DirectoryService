using Amazon.S3;

namespace FileService.Infrastructure.S3;

public class S3Provider
{
    private readonly IAmazonS3 _s3Client;

    public S3Provider(IAmazonS3 s3Client)
    {
        _s3Client = s3Client;
    }

    public Task UploadFileAsync(Stream stream)
    {
        return Task.CompletedTask;
    }
}
