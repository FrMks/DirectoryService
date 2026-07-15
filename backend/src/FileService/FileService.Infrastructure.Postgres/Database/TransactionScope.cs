using System.Data;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Core.Database;

namespace FileService.Infrastructure.Postgres.Database;

/// <summary>
/// Представляет область жизни транзакции.
/// </summary>
// При создании transaction scope внутри него делаем несколько действий,
// если все хорошо - commit, если плохо rollback, в конце Dispose.
public class TransactionScope : ITransactionScope, IDisposable
{
    // Открытая транзакция соединения с БД
    private readonly IDbTransaction _transaction;
    private readonly ILogger _logger;

    public TransactionScope(IDbTransaction transaction, ILogger<TransactionScope> logger)
    {
        _transaction = transaction;
        _logger = logger;
    }

    // Commit не пушит все данные в нужные места сам по себе
    // Данные попадают в БД через SaveChangesAsync
    // Но если есть открытая transaction, эти изменения находятся внутри транзакции
    // Потом Commit говорит БД: Зафиксировать все изменения этой транзакции окончательно
    public UnitResult<Error> Commit()
    {
        try
        {
            _transaction.Commit();
            return UnitResult.Success<Error>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to commit transaction");
            return Error.Failure("transaction.commit.failed", "Failed to commit transaction");
        }
    }

    public void Dispose()
    {
        _transaction.Dispose();
    }

    public UnitResult<Error> Rollback()
    {
        try
        {
            _transaction.Rollback();
            return UnitResult.Success<Error>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to rollback transaction");
            return UnitResult.Failure(Error.Failure(
                "transaction.rollback.failed",
                "Failed to rollback transaction"));
        }
    }
}