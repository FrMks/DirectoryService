using CSharpFunctionalExtensions;
using DirectoryService.Application.Positions.Interfaces;
using DirectoryService.Domain.Positions;
using DirectoryService.Domain.Positions.ValueObject;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Infrastructure.Postgres.Repositories;

public class PositionsRepository(DirectoryServiceDbContext dbContext, ILogger<PositionsRepository> logger) : IPositionsRepository
{
    public Task<Result<Guid, Error>> AddAsync(Position position, CancellationToken cancellationToken) => throw new NotImplementedException();

    public async Task<Result<bool, Error>> IsNameExistAndNotActive(Name name, CancellationToken cancellationToken)
    {
        var position = await dbContext.Positions.FirstOrDefaultAsync(p => p.Name == name, cancellationToken);

        if (position is null)
            return true;
        
        if (position.IsActive)
            return Error.Failure("department.failure", $"Department with id: {position.Id} is active.");

        return true;
    }
}