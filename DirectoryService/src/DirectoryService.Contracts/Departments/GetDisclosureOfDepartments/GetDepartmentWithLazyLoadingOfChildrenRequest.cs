using DirectoryService.Contracts.Locations.GetLocations;

namespace DirectoryService.Contracts.Departments.GetDisclosureOfDepartments;

public record GetDepartmentWithLazyLoadingOfChildrenRequest(Guid DepartmentId, PaginationRequest? Pagination = null);