using CSharpFunctionalExtensions;
using DirectoryService.Domain.Department;
using Shared;

namespace DirectoryService.Application.Departments.Interfaces;

public interface IDepartmentsRepository
{
    Task<Result<Guid, Error>> AddAsync(Department department, CancellationToken cancellationToken);
}