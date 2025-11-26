using DirectoryService.Application.Abstractions;
using DirectoryService.Contracts.Departments;

namespace DirectoryService.Application.Departments;

public record UpdateDepartmentLocationsCommand(Guid DepartmentId, UpdateDepartmentLocationsRequest DepartmentLocationsRequest) : ICommand;