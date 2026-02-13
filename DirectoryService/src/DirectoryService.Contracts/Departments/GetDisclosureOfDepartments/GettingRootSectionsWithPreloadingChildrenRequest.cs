using DirectoryService.Contracts.Locations.GetLocations;

namespace DirectoryService.Contracts.Departments.GetDisclosureOfDepartments;

public record GettingRootSectionsWithPreloadingChildrenRequest
(
    PaginationRequest? Pagination = null,
    int? Prefetch = 3
);