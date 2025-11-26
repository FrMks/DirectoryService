using CSharpFunctionalExtensions;
using DirectoryService.Domain.Department;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.Locations.ValueObjects;
using DirectoryService.Domain.ValueObjects;
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

    /// <summary>
    /// Check that all elements in List<Guid> have in a database and they are active.
    /// </summary>
    /// <param name="departmentIds">Guid for a searching element id database.</param>
    /// <param name="cancellationToken">Token to cancel.</param>
    /// <returns>Erorr - does not have in database or not active. True - have in database and active.</returns>
    Task<Result<bool, Error>> AllExistAndActiveAsync(List<Guid> departmentIds, CancellationToken cancellationToken);

    /// <summary>
    /// Update list of location in department.
    /// </summary>
    /// <param name="departmentId">Identification for search department for updating.</param>
    /// <param name="locationIds">List locations that should be in the department.</param>
    /// <param name="cancellationToken">Token to cancel.</param>
    /// <returns></returns>
    Task<Result<Guid, Error>> UpdateLocationsAsync(
        DepartmentId departmentId,
        List<LocationId> locationIds,
        DepartmentLocationId departmentLocationId,
        CancellationToken cancellationToken);
}