using CSharpFunctionalExtensions;
using Shared;

namespace DirectoryService.Application.Database;

public interface ITransactionManager
{
    Task<Result<ITransactionScope, Error>> BeginTransactionAsTask(CancellationToken cancellationToken);
    Task<UnitResult<Error>> SaveChangesAsync(CancellationToken cancellationToken);
}