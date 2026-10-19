using FileService.Core.Event;
using FileService.Domain.IntegrationEvents;
using Microsoft.EntityFrameworkCore;

namespace FileService.Infrastructure.Postgres.Repositories;

public class ProcessedFileEventRepository : IProcessedFileEventRepository
{
    private readonly FileServiceDbContext _dbContext;

    public ProcessedFileEventRepository(FileServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> TryRegisterAsync(
        Guid eventId,
        string eventType,
        CancellationToken cancellationToken)
    {
        bool hasEventInDb = await _dbContext.ProcessedFileEvent
            .AnyAsync(x => x.EventId == eventId, cancellationToken);

        if (hasEventInDb)
            return false;

        await _dbContext.AddAsync(
            new ProcessedFileEvent(eventId, eventType, DateTime.UtcNow),
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}