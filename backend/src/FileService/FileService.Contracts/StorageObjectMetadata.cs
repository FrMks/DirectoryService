namespace FileService.Contracts;

public record StorageObjectMetadata(
    string ContentType,
    long SizeBytes,
    string? ETag);