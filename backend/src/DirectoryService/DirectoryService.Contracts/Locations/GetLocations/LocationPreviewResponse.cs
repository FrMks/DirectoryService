namespace DirectoryService.Contracts.Locations.GetLocations;

public record LocationPreviewResponse(
    Guid? AssetId,
    string Status,
    string? FileName,
    string? ContentType,
    long? Size,
    string? ContentUrl,
    string? Message);