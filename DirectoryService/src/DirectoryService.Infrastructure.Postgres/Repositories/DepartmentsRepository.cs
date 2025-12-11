using CSharpFunctionalExtensions;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Domain.Department;
using DirectoryService.Domain.Department.ValueObject;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;
using Path = DirectoryService.Domain.Department.ValueObject.Path;

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
    
    public async Task<Result<Department, Errors>> ExistAndActiveAsync(DepartmentId departmentId, CancellationToken cancellationToken)
    {
        var department = await GetByIdAsync(departmentId, cancellationToken);

        if (department.IsFailure)
            return department.Error;
        
        if (!department.Value.IsActive)
        {
            return Error.Failure(
                "department.is.not.active",
                $"Department with id: {department.Value.Id} is not active").ToErrors();
        }

        return department;
    }

    public async Task<Result<Guid, Error>> SaveChanges(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success<Guid, Error>(Guid.NewGuid());
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error saving changes");
            return Error.Failure(null, "Database error occurred.");
        }
    }

    public async Task<Result<Department, Error>> GetByIdWithLock(
        DepartmentId departmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var department = await dbContext.Departments
                .FromSql($"SELECT * FROM departments WHERE id = {departmentId.Value} FOR UPDATE")
                .FirstOrDefaultAsync(cancellationToken);

            if (department is null)
            {
                logger.LogError("Department with id {departmentId} not found", departmentId.Value);
                return Error.NotFound(
                    "department.not.found",
                    "Department with id: " + departmentId.Value + " not found.",
                    departmentId.Value);
            }

            return department;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error getting active department {departmentId}", departmentId.Value);
            return Error.Failure("department.by.id", $"Department with id: {departmentId} not found.");
        }
    }

    public async Task<UnitResult<Error>> MoveDepartmentWithChildren(
        string oldPath,
        string newPath,
        Guid? newParentId,
        CancellationToken cancellationToken)
    {
        try
        {
            // subpath(path, nlevel({oldPath}::ltree)) - получаем хвост (it.devops => devops) 
            await dbContext.Database.ExecuteSqlAsync(
                $"""
                     UPDATE departments
                     SET path = {newPath}::ltree || subpath(path, nlevel({oldPath}::ltree)),
                         depth = nlevel({newPath}::ltree || subpath(path, nlevel({oldPath}::ltree))) - 1,
                         parent_id = CASE
                             WHEN path = {oldPath}::ltree THEN {newParentId} 
                             ELSE parent_id
                         END,
                         updated_at = NOW()
                     WHERE path <@ {oldPath}::ltree
                 """,
                cancellationToken);

            return UnitResult.Success<Error>();
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "Failed to move department from {oldPath} to {newPath}",
                oldPath,
                newPath);
            return Error.Failure(
                "department.move.failed",
                $"Failed to move department: {e.Message}");
        }
    }
}