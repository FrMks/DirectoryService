namespace FileService.Communication;

public record FileServiceOptions
{
    public const string SectionName = "FileService";

    public string Url { get; init; } = string.Empty;

    public int TimeoutSeconds { get; init; } = 7;
}