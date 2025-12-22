namespace DirectoryService.Contracts.Locations.GetLocations;

public record GetLocationsResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string Address { get; init; } = null!;

    public string Timezone { get; init; } = null!;

    public bool IsActive { get; init; }
    
    public DateTime CreatedAt { get; init; }
    
    public DateTime UpdatedAt { get; init; }
}