using Shared.Core.Abstractions;

namespace FileService.Core.Files;

public record UploadFileCommand(
    Stream Stream,
    string BucketName,
    string Key,
    string ContentType) : ICommand;
