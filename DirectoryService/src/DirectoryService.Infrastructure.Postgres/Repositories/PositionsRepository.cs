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

public class PositionsRepository(DirectoryServiceDbContext dbContext, ILogger<PositionsRepository> logger) : IPositionsRepository
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

    public async Task<Result<bool, Error>> HasOtherActiveDepartmentsForPosition(
        PositionId positionId,
        DepartmentId deletingDepartmentId,
        CancellationToken cancellationToken)
    {
        var positionExists = await dbContext.Positions
            .AnyAsync(p => p.Id == positionId, cancellationToken);
        if (!positionExists)
        {
            logger.LogError("Position with id {PositionId} not found", positionId.Value);
            return Error.NotFound(
                "position.not.found",
                $"Position not found.",
                positionId.Value);
        }

        var hasOtherActiveDepartments = await dbContext.DepartmentPositions
            .Join(
                dbContext.Departments,
                dp => dp.DepartmentId,
                d => d.Id,
                (dp, d) => new { DepartmentPosition = dp, Department = d })
            .AnyAsync(
                x => x.DepartmentPosition.PositionId == positionId &&
                     x.DepartmentPosition.DepartmentId != deletingDepartmentId &&
                     x.Department.IsActive,
                cancellationToken);

        return hasOtherActiveDepartments;
    }
}
