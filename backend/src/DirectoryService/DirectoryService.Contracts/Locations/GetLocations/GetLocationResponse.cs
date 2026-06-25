namespace DirectoryService.Contracts.Locations.GetLocations;

public record GetLocationResponse(
    Guid Id,
    string Name,
    string Street,
    string City,
    string Country,
    string Timezone,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    LocationPreviewResponse Preview);
