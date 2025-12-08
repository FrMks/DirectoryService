namespace DirectoryService.Contracts.Departments;

public record UpdateDepartmentLocationsRequest(List<Guid> LocationsIds);