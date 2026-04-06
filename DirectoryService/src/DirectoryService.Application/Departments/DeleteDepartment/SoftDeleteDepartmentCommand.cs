using DirectoryService.Application.Abstractions;

namespace DirectoryService.Application.Departments.SoftDeleteDepartment;

public record SoftDeleteDepartmentCommand(Guid DepartmentId) : ICommand;