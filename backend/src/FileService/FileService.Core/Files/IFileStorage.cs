namespace FileService.Core.Files;

public interface IFileStorage
{
    Task UploadFileAsync(
        Stream stream,
        string bucketName,
        string key,
        string contentType,
        CancellationToken cancellationToken);
}
