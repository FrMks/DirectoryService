using Shared.Core.Abstractions;
using DirectoryService.Contracts.Departments;

namespace DirectoryService.Application.Departments;

public record CreateDepartmentCommand(CreateDepartmentRequest DepartmentRequest) : ICommand;