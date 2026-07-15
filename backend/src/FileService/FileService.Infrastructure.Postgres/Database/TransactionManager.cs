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
            var transaction = await _dbContext.Database.BeginTransactionAsync(
                level ?? System.Data.IsolationLevel.ReadCommitted,
                cancellationToken);
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