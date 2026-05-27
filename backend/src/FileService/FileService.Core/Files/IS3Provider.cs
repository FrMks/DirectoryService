using CSharpFunctionalExtensions;
using FileService.Contracts;
using Shared;

namespace FileService.Core.Files;

public interface IS3Provider
{
    Task<Result<string, Error>> CompleteMultipartUploadAsync(string bucketName, string key, string uploadId, IReadOnlyList<PartETagDto> partETags, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<string>, Error>> GenerateAllChunkUploadUrlsAsync(string bucketName, string key, string uploadId, int totalChunks, CancellationToken cancellationToken);
    Task<Result<string?, Error>> GenerateDownloadUrlAsync(string bucketName, string key);
    Task<Result<string?, Error>> GenerateUploadUrlAsync(string bucketName, string key);
    Task<Result<string, Error>> StartMultipartUploadAsync(string bucketName, string key, string contentType, CancellationToken cancellationToken);
    Task UploadFileAsync(Stream stream, string bucketName, string key, string contentType, CancellationToken cancellationToken);
}
