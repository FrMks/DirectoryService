using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using DirectoryService.Application.Positions.Interfaces;
using DirectoryService.Domain.Positions;
using DirectoryService.Domain.Positions.ValueObject;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;
using DepartmentId = DirectoryService.Domain.Department.ValueObject.DepartmentId;

namespace DirectoryService.Infrastructure.Postgres.Repositories;

public class PositionsRepository(DirectoryServiceDbContext dbContext, ILogger<PositionsRepository> logger)
     : IPositionsRepository
{
    public async Task<Result<Guid, Error>> AddAsync(Position position, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Positions.AddAsync(position, cancellationToken);
            
            await dbContext.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Successfully added to the database with id {position}", position.Id.Value);
        }
        catch (Exception e)
        {
            return Error.Failure(
                "positions.repository.failure",
                "Database error occurred when add position to a database.");
        }
        
        return Result.Success<Guid, Error>(position.Id.Value);
    }

    public async Task<Result<bool, Error>> IsNameExistAndNotActive(Name name, CancellationToken cancellationToken)
    {
        var position = await dbContext.Positions.FirstOrDefaultAsync(p => p.Name == name, cancellationToken);

        if (position is null)
            return true;
        
        if (position.IsActive)
            return Error.Failure("position.failure", $"Position with id: {position.Id.Value} is active.");

        return true;
    }

    public async Task<Result<Position, Error>> GetBy(
        Expression<Func<Position, bool>> predicate,
        CancellationToken cancellationToken)
    {
        var position = await dbContext.Positions.FirstOrDefaultAsync(predicate, cancellationToken);

        if (position is null)
        {
            logger.LogError("Position not found with given predicate");
            return Error.NotFound(
                "position.not.found",
                $"Position not found.",
                null);
        }

        return position;
    }

    public async Task<Result<List<Position>, Error>> GetPositionsByIds(
        List<PositionId> positionIds,
        CancellationToken cancellationToken)
    {
        if (positionIds.Count == 0)
        {
            return new List<Position>();
        }

        var positions = await dbContext.Positions
            .Where(p => positionIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (positions.Count != positionIds.Count)
        {
            logger.LogError("Not all positions found with the given IDs");
            return Error.NotFound(
                "positions.not.found",
                "Not all positions found with the given IDs.",
                null);
        }

        return positions;
    }

    public async Task<Result<HashSet<PositionId>, Error>> GetPositionIdsWithOtherActiveDepartments(
        List<PositionId> positionIds,
        DepartmentId deletingDepartmentId,
        CancellationToken cancellationToken)
    {
        if (positionIds.Count == 0)
        {
            return new HashSet<PositionId>();
        }

        var existingPositionIds = await dbContext.Positions
            .Where(p => positionIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (existingPositionIds.Count != positionIds.Count)
        {
            logger.LogError("Not all positions found with the given IDs");
            return Error.NotFound(
                "positions.not.found",
                "Not all positions found with the given IDs.",
                null);
        }

        var positionIdsWithOtherActiveDepartments = await dbContext.DepartmentPositions
            .Join(
                dbContext.Departments,
                dp => dp.DepartmentId,
                d => d.Id,
                (dp, d) => new { DepartmentPosition = dp, Department = d })
            .Where(
                x => positionIds.Contains(x.DepartmentPosition.PositionId) &&
                     x.DepartmentPosition.DepartmentId != deletingDepartmentId &&
                     x.Department.IsActive)
            .Select(x => x.DepartmentPosition.PositionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return positionIdsWithOtherActiveDepartments.ToHashSet();
    }

    public async Task<UnitResult<Error>> SoftDeleteUnusedPositionsInBranchAsync(
        Domain.Department.ValueObject.Path branchPath,
        CancellationToken cancellationToken)
    {
        try
        {
    #pragma warning disable EF1002
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                WITH deleting_departments AS (
                    SELECT id
                    FROM departments
                    WHERE path <@ {branchPath.Value}::ltree
                ),
                branch_positions AS (
                    SELECT DISTINCT dp.position_id
                    FROM department_positions AS dp
                    JOIN deleting_departments AS dd ON dd.id = dp.department_id
                ),
                positions_used_outside_branch AS (
                    SELECT DISTINCT dp.position_id
                    FROM department_positions AS dp
                    JOIN departments AS d ON d.id = dp.department_id
                    WHERE dp.position_id IN (SELECT position_id FROM branch_positions)
                    AND d.is_active = TRUE
                    AND dp.department_id NOT IN (SELECT id FROM deleting_departments)
                )
                UPDATE positions AS p
                SET is_active = FALSE,
                    deleted_at = NOW(),
                    update_at = NOW()
                WHERE p.id IN (
                    SELECT position_id
                    FROM branch_positions
                    EXCEPT
                    SELECT position_id
                    FROM positions_used_outside_branch
                );
                """,
                cancellationToken);
    #pragma warning restore EF1002

            return UnitResult.Success<Error>();
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "Failed to soft delete unused positions in branch {BranchPath}",
                branchPath.Value);

            return Error.Failure(
                "positions.soft.delete.branch.failed",
                "Failed to soft delete unused positions in branch.");
        }
    }
}
