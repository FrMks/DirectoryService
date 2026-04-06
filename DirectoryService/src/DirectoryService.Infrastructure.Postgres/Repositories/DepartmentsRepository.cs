using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Contracts.Departments;
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
        // Без Include у меня достается только таблица departments (не происходит join с department_locations
        var department = await dbContext.Departments
            .Include(d => d.DepartmentLocations)
            .FirstOrDefaultAsync(d => d.Id == departmentId, cancellationToken);

        if (department is null)
        {
            return Error.NotFound(
                "department.not.found",
                $"Department with id: {departmentId} not found.",
                departmentId.Value).ToErrors();
        }

        if (!department.IsActive)
        {
            return Error.Failure(
                "department.is.not.active",
                $"Department with id: {department.Id} is not active").ToErrors();
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
        Path oldPath,
        Path newPath,
        Guid? newParentId,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Database.ExecuteSqlAsync(
                $"""
                     UPDATE departments
                     SET path = CASE 
                             WHEN nlevel(path) > nlevel({oldPath.Value}::ltree) 
                             THEN {newPath.Value}::ltree || subpath(path, nlevel({oldPath.Value}::ltree))
                             ELSE {newPath.Value}::ltree
                         END,
                         depth = CASE 
                             WHEN nlevel(path) > nlevel({oldPath.Value}::ltree) 
                             THEN nlevel({newPath.Value}::ltree || subpath(path, nlevel({oldPath.Value}::ltree))) - 1
                             ELSE nlevel({newPath.Value}::ltree) - 1
                         END,
                         parent_id = CASE
                             WHEN path = {oldPath.Value}::ltree THEN {newParentId} 
                             ELSE parent_id
                         END,
                         updated_at = NOW()
                     WHERE path <@ {oldPath.Value}::ltree
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

    public async Task<Result<Department, Error>> GetBy(
        Expression<Func<Department, bool>> predicate,
        CancellationToken cancellationToken)
    {
        var department = await dbContext.Departments.FirstOrDefaultAsync(predicate, cancellationToken);

        if (department is null)
        {
            logger.LogError("Department not found with given predicate");
            return Error.NotFound(
                "department.not.found",
                $"Department not found.",
                null);
        }

        return department;
    }

    public async Task<Result<List<Department>, Error>> GetListBy(
        Expression<Func<Department, bool>> predicate,
        CancellationToken cancellationToken)
    {
        var departments = await dbContext.Departments.Where(predicate).ToListAsync(cancellationToken);

        if (departments is null || !departments.Any())
        {
            logger.LogError("Departments not found with given predicate");
            return Error.NotFound(
                "departments.not.found",
                $"Departments not found.",
                null);
        }

        return departments;
    }

    public async Task<Result<Department, Error>> GetActiveDepartmentForSoftDelete(DepartmentId departmentId, CancellationToken cancellationToken)
    {
        var department = await dbContext.Departments
            .Include(d => d.DepartmentLocations)
            .Include(d => d.DepartmentPositions)
            .FirstOrDefaultAsync(d => d.Id == departmentId && d.IsActive, cancellationToken);

        if (department is null)
        {
            logger.LogError("Active department with id {DepartmentId} for soft delete not found", departmentId.Value);
            return
                Error.NotFound(
                    "active.department.for.soft.delete.not.found",
                    $"Active department with id: {departmentId} for soft delete not found.",
                    departmentId.Value);
        }

        return department;
    }

    public async Task<UnitResult<Error>> CleanupDeletedDepartmentsOlderThan(
        List<DepartmentIdPathAndParentId> departmentIdPathAndParentIds,
        CancellationToken cancellationToken)
    {
        if (departmentIdPathAndParentIds.Count == 0)
        {
            return UnitResult.Success<Error>();
        }

        try
        {
            // Получили Id всех удаляемых департаментов
            var departmentIds = departmentIdPathAndParentIds
                .Select(x => x.DepartmentId.Value)
                .Distinct()
                .ToArray();

            // Склеиваем все параметры у класса DepartmentIdPathAndParentId в одну строку для передачи в SQL запрос
            // На выходе будет строка вида: ('{DepartmentId1}'::uuid, {ParentId1}, '{Path1}'), ...
            var deletedDepartmentsValues = string.Join(
                ", ",
                departmentIdPathAndParentIds.Select(x =>
                    $"('{x.DepartmentId.Value}'::uuid, {(x.ParentId is null ? "NULL::uuid" : $"'{x.ParentId.Value}'::uuid")}, '{x.Path.Value}')"));

#pragma warning disable EF1002
            // Создаем временную таблицу с значениями для удаляемых департаментов
            // Находим всех детей из таблицы departments, у которых parent_id совпадает с id удаляемого департамента
            // У этих детей меняем parent_id на parent_id удаляемого департамента и обновляем updated_at
            await dbContext.Database.ExecuteSqlRawAsync(
                $"""
                    WITH deleted_departments(id, parent_id, path) AS (
                        VALUES {deletedDepartmentsValues}
                    )

                    UPDATE departments AS child
                    SET parent_id = deleted_departments.parent_id,
                        updated_at = NOW()
                    FROM deleted_departments
                    WHERE child.parent_id = deleted_departments.id;
                 """,
                cancellationToken);

            // Создаем временную таблицу с значениями для удаляемых департаментов
            // Обновляем таблицу departments
            // У всех детей удаляемого департамента меняем path, depth и updated_at
            // Логика обновления path:
            // Если удаляемый департамент был корневым
            // (nlevel(path) показывает количество сегментов в path, для корневого path это 1),
            // то новый path строится через subpath(d.path, nlevel(deleted_departments.path::ltree))
            // Это означает: "отрезать от текущего path первый сегмент"
            //
            // Иначе, если удаляемый департамент не был корневым
            // path собирается из двух частей:
            // 1. subpath(d.path, 0, nlevel(deleted_departments.path::ltree) - 1)
            // - берем часть path от начала до уровня удаляемого департамента, То есть от 0 до nlevel(deleted_departments.path::ltree) - 1.
            // 2. subpath(d.path, nlevel(deleted_departments.path::ltree))
            // - берем хвост path после удаляемого департамента. То есть с nlevel(deleted_departments.path::ltree) по конец.
            // Затем эти две части склеиваются оператором ||.
            // Удаляемый сегмент исчезает из path
            //
            // Для depth
            // Если Корневой, то убираем первый сегмент и считаем количество оставшихся сегментов, отнимая 1, так как depth считается от 0
            // Иначе, если не Корневой, то собираем новый path без удаляемого сегмента и считаем количество сегментов в новом path, отнимая 1, так как depth считается от 0
            //
            // В WHERE:
            // d.path <@ deleted_departments.path::ltree 
            // 'hq.deleted_it.dev_team' <@ 'hq.deleted_it' = true
            // означает: берем все узлы внутри поддерева удаляемого департамента (включая его самого)
            // d.path != deleted_departments.path::ltree не обновляем сам удаляемый департамент,
            // потому что он будет удален позже отдельным DELETE.
            await dbContext.Database.ExecuteSqlRawAsync(
                $"""
                WITH deleted_departments(id, parent_id, path) AS (
                    VALUES {deletedDepartmentsValues}
                )
                UPDATE departments AS d
                SET path = CASE
                        WHEN nlevel(deleted_departments.path::ltree) = 1
                            THEN subpath(d.path, nlevel(deleted_departments.path::ltree))
                        ELSE
                            subpath(d.path, 0, nlevel(deleted_departments.path::ltree) - 1)
                            || subpath(d.path, nlevel(deleted_departments.path::ltree))
                    END,
                    depth = CASE
                        WHEN nlevel(deleted_departments.path::ltree) = 1
                            THEN nlevel(subpath(d.path, nlevel(deleted_departments.path::ltree))) - 1
                        ELSE
                            nlevel(
                                subpath(d.path, 0, nlevel(deleted_departments.path::ltree) - 1)
                                || subpath(d.path, nlevel(deleted_departments.path::ltree))
                            ) - 1
                    END,
                    updated_at = NOW()
                FROM deleted_departments
                WHERE d.path <@ deleted_departments.path::ltree
                  AND d.path != deleted_departments.path::ltree;
                """,
                cancellationToken);
            // Удаляем строки из таблицы department_locations, department_positions и departments 
            // для всех удаляемых департаментов и их детей, которые мы получили в первом запросе через departmentIds
            await dbContext.Database.ExecuteSqlRawAsync(
                $"""
                WITH deleted_departments(id, parent_id, path) AS (
                    VALUES {deletedDepartmentsValues}
                )
                DELETE FROM department_locations AS dl
                USING deleted_departments
                WHERE dl.department_id = deleted_departments.id;
                """,
                cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(
                $"""
                WITH deleted_departments(id, parent_id, path) AS (
                    VALUES {deletedDepartmentsValues}
                )
                DELETE FROM department_positions AS dp
                USING deleted_departments
                WHERE dp.department_id = deleted_departments.id;
                """,
                cancellationToken);

            var deletedDepartmentsCount = await dbContext.Database.ExecuteSqlRawAsync(
                $"""
                WITH deleted_departments(id, parent_id, path) AS (
                    VALUES {deletedDepartmentsValues}
                )
                DELETE FROM departments AS d
                USING deleted_departments
                WHERE d.id = deleted_departments.id;
                """,
                cancellationToken);

            logger.LogInformation(
                "Department cleanup delete step removed {DeletedDepartmentsCount} departments.",
                deletedDepartmentsCount);
#pragma warning restore EF1002

            return UnitResult.Success<Error>();
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "Failed to hard delete departments older than cleanup threshold.");

            return Error.Failure(
                "department.cleanup.delete.failed",
                "Failed to hard delete departments.");
        }
    }
}
