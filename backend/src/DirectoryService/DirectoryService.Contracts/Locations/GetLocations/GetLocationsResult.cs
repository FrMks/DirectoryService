namespace DirectoryService.Contracts.Locations.GetLocations;

public record GetLocationsResult(
    List<GetLocationsResponse> Items,
    long TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
