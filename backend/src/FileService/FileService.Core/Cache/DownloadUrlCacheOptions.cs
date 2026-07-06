namespace FileService.Core.Cache;

public sealed record DownloadUrlCacheOptions
{
    public const string SectionName = "DownloadUrlCache";

    public int ExpirationMinutes { get; init; } = 35;
}