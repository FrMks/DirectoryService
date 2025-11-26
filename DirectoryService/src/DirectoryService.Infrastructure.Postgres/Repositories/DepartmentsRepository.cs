using CSharpFunctionalExtensions;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Domain;
using DirectoryService.Domain.Department;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.Locations.ValueObjects;
using DirectoryService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Infrastructure.Postgres.Repositories;

public class DepartmentsRepository(DirectoryServiceDbContext dbContext, ILogger<DepartmentsRepository> logger)
    : IDepartmentsRepository
{
    public async Task<Result<Guid, Error>> AddAsync(Department department, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Departments.AddAsync(department, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken); // Применяем изменения
        }
        catch (Exception e)
        {
            return Error.Failure(null, "Database error occurred.");
        }
        
        return Result.Success<Guid, Error>(department.Id.Value);
    }

    public async Task<Result<Department, Errors>> GetByIdAsync(DepartmentId id, CancellationToken cancellationToken)
    {
        var department = await dbContext.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (department is null)
            return Error.NotFound(null, $"Department with id: {id} not found.", id.Value).ToErrors();

        return department;
    }

    public async Task<Result<bool, Error>> IsIdentifierIsUniqueAsync(
        Identifier identifier,
        CancellationToken cancellationToken)
    {
        var haveDepartmentInDatabaseWithSameIdentifier = await dbContext.Departments
            .AnyAsync(d => d.Identifier == identifier, cancellationToken);
        
        if (haveDepartmentInDatabaseWithSameIdentifier)
        {
            return Error.Failure(
                "identifier.have.in.database",
                $"Department with {identifier.Value} have in database");
        }

        return true;
    }

    public async Task<Result<bool, Error>> AllExistAndActiveAsync(List<Guid> departmentIds,
        CancellationToken cancellationToken)
    {
        var departments = await dbContext.Departments
            .Where(d => departmentIds.Contains(d.Id))
            .ToListAsync(cancellationToken);

        if (departments.Count != departmentIds.Count)
        {
            return Error.Failure(
                "department.not.failure",
                $"Some department id does not have in database");
        }
        
        var departmentsNotActive = departments
            .Where(d => !d.IsActive)
            .ToList();

        if (departmentsNotActive.Any())
        {
            return Error.Failure(
                "department.failure",
                $"Some departments are not active");
        }

        return true;
    }

    public Task<Result<Guid, Error>> UpdateLocationsAsync(DepartmentLocationId departmentLocationId, Guid departmentId, List<Guid> locationIds,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public async Task<Result<Guid, Error>> UpdateLocationsAsync(
        DepartmentId departmentId,
        List<LocationId> locationIds,
        DepartmentLocationId departmentLocationId,
        CancellationToken cancellationToken)
    {
        // await dbContext.Departments
        //     .Where(d => d.Id == departmentId)
        //     .ExecuteUpdateAsync(
        //         setter =>
        //             setter.SetProperty(
        //                 d => d.DepartmentLocations,
        //                 DepartmentLocation.Create(departmentLocationId, departmentId, locationIds[0]).Value),
        //         cancellationToken: cancellationToken);

        return departmentId.Value;
    }
}