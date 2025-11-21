using CSharpFunctionalExtensions;
using DirectoryService.Domain.Department;
using DirectoryService.Domain.Department.ValueObject;
using Shared;

namespace DirectoryService.Application.Departments.Interfaces;

public interface IDepartmentsRepository
{
    Task<Result<Guid, Error>> AddAsync(Department department, CancellationToken cancellationToken);
    Task<Result<Department, Errors>> GetByIdAsync(DepartmentId id, CancellationToken cancellationToken);
}