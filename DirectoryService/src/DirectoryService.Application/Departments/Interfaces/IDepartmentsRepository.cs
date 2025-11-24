using CSharpFunctionalExtensions;
using DirectoryService.Domain.Department;
using DirectoryService.Domain.Department.ValueObject;
using Shared;

namespace DirectoryService.Application.Departments.Interfaces;

public interface IDepartmentsRepository
{
    Task<Result<Guid, Error>> AddAsync(Department department, CancellationToken cancellationToken);
    Task<Result<Department, Errors>> GetByIdAsync(DepartmentId id, CancellationToken cancellationToken);
    
    /// <summary>
    /// Check that department with identifier does not have in a database.
    /// </summary>
    /// <param name="identifier">By that identifier check unique.</param>
    /// <param name="cancellationToken">Token to cancel.</param>
    /// <returns>True - does not have a department with that identifier in a database.
    /// Error - have in a database.</returns>
    Task<Result<bool, Error>> IsIdentifierIsUniqueAsync(Identifier identifier, CancellationToken cancellationToken);
}