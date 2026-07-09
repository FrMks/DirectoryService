namespace FileService.Domain.ValueObjects;

public record HlsResult
{
    private HlsResult()
    {
    }

    public HlsResult(StorageKey manifestKey)
    {
        ManifestKey = manifestKey;
    }

    public StorageKey ManifestKey { get; private set; } = null!; // point to master.m3u8 file
}