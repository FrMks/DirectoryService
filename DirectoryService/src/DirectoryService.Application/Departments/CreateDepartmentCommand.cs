using DirectoryService.Application.Abstractions;
using DirectoryService.Contracts.Departments;

namespace DirectoryService.Application.Departments;

/// <summary>
/// Введен CreateDepartmentCommand в слое Application
/// для удаления прямой зависимости от CreateDepartmentRequest из проекта Contracts.
/// Это обеспечивает более строгие границы между слоями и соответствует принципам CQRS.
/// </summary>
/// <param name="DepartmentRequest"></param>
public record CreateDepartmentCommand(CreateDepartmentRequest DepartmentRequest) : ICommand;