using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using DirectoryService.Domain.Positions;
using DirectoryService.Domain.Positions.ValueObject;
using Shared;

namespace DirectoryService.Application.Positions.Interfaces;

public interface IPositionsRepository
{
    Task<Result<Guid, Error>> AddAsync(Position position, CancellationToken cancellationToken);
    
    Task<Result<bool, Error>> IsNameExistAndNotActive(Name name, CancellationToken cancellationToken);

    Task<Result<Position, Error>> GetBy(Expression<Func<Position, bool>> predicate, CancellationToken cancellationToken);
}