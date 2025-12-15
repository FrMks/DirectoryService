using System.Data;
using CSharpFunctionalExtensions;
using Shared;

namespace DirectoryService.Application.Database;

public interface ITransactionManager
{
    Task<Result<ITransactionScope, Error>> BeginTransaction(
        CancellationToken cancellationToken = default,
        IsolationLevel? level = null);
    Task<UnitResult<Error>> SaveChangesAsync(CancellationToken cancellationToken);
}