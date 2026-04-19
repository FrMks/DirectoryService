using Shared.Core.Abstractions;
using DirectoryService.Contracts.Departments;

namespace DirectoryService.Application.Departments;

public record UpdateParentLevelCommand(Guid DepartmentId, UpdateParentLevelRequest ParentLevelRequest) : ICommand;