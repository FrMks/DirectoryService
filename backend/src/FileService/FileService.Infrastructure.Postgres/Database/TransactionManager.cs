using CSharpFunctionalExtensions;
using Shared.Core.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Shared;

namespace FileService.Infrastructure.Postgres.Database;

public class TransactionManager : ITransactionManager
{
    private readonly FileServiceDbContext _dbContext;
    private readonly ILogger<TransactionManager> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public TransactionManager(
        FileServiceDbContext dbContext,
        ILogger<TransactionManager> logger,
        ILoggerFactory loggerFactory)
    {
        _dbContext = dbContext;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task<Result<ITransactionScope, Error>> BeginTransaction(
        CancellationToken cancellationToken = default,
        System.Data.IsolationLevel? level = null)
    {
        try
        {
            // IsolationLevel определяет на сколько транзакция изолирована от других параллельных транзакций
            // Пример: два пользователя одновременно меняют данные. Isolation level отвечает на вопросы:
            // вижу ли я чужие незакоммиченные изменения?
            // могут ли данные измениться, пока я читаю?
            // могу ли я получить разные результаты одного и того же запроса внутри одной транзакции?
            // ReadCommitted - транзакция видит только те данные, которые уже были commit-нуты другими транзакциями
            IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(
                level ?? System.Data.IsolationLevel.ReadCommitted,
                cancellationToken);
            // Создаем логгер ИМЕННО ДЛЯ КЛАССА TransactionScope
            // берем EF Transaction и достаем из нее обычную DbTransaction
            // оборачиваем эту DbTransaction в наш TransactionScope
            ILogger<TransactionScope> transactionScopeLogger = _loggerFactory.CreateLogger<TransactionScope>();
            var transactionScope = new TransactionScope(transaction.GetDbTransaction(), transactionScopeLogger);

            return transactionScope;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to begin transaction");
            return Error.Failure("database", "Failed to begin transaction");
        }
    }

    public async Task<UnitResult<Error>> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return UnitResult.Success<Error>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to save changes");
            return Error.Failure("database", "Failed to save changes");
        }
    }
}