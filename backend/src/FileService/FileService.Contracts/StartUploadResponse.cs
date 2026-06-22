namespace FileService.Contracts;

public record StartUploadResponse(
    Guid MediaAssetId,
    string UploadUrl,
    string Method,
    DateTimeOffset ExpiresAt,
    IReadOnlyDictionary<string, string> RequiredHeaders);