namespace FileService.Contracts;

public record GetContentUrlResponse(
    Guid MediaAssetId,
    string Url,
    string Method,
    DateTimeOffset ExpiresAt);