namespace FileService.Domain.ValueObjects;

public record HlsResult
{
    public HlsResult(StorageKey manifestKey)
    {
        ManifestKey = manifestKey;
    }

    private HlsResult()
    {
    }

    public StorageKey ManifestKey { get; private set; } = null!; // point to master.m3u8 file
}