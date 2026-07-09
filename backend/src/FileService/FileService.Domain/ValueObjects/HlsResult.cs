namespace FileService.Domain.ValueObjects;

public record HlsResult(
    StorageKey ManifestKey // point to master.m3u8 file
);