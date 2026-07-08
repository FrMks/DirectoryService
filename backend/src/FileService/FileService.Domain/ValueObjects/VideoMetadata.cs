namespace FileService.Domain.ValueObjects;

public sealed record VideoMetadata(
    TimeSpan? Duration,
    int? Width,
    int? Height,
    string? Codec,
    string? Containter);