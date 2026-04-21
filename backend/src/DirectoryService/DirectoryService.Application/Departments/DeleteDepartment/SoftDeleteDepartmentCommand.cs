using Shared.Core.Abstractions;

namespace DirectoryService.Application.Departments.SoftDeleteDepartment;

public record SoftDeleteDepartmentCommand(Guid DepartmentId) : ICommand;