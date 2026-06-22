namespace FileService.Contracts;

public record FileResponse
(
    Guid Id,
    string FileName,
    string ContentType,
    long Size,
    string Status,
    string AssetType,
    string Context,
    Guid ContextId,
    string? ContentUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt
);