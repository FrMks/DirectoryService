using FileService.Core.Event;
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
        int affectedRows = await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO processed_file_events (event_id, event_type, processed_at)
            VALUES ({eventId}, {eventType}, {DateTime.UtcNow})
            ON CONFLICT (event_id) DO NOTHING;
            """, cancellationToken);

        return affectedRows == 1;
    }
}
