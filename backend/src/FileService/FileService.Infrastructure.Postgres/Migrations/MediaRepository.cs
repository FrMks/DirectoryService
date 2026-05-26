using FileService.Core;
using FileService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FileService.Infrastructure.Postgres.Migrations;

public class MediaRepository(FileServiceDbContext dbContext, ILogger<MediaRepository> logger) : IMediaRepository
{
    public async Task AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.MediaAssets.AddAsync(mediaAsset, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully added to the database with id{mediaAsset}", mediaAsset.Id);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Database error occurred when added media asset to a database.");
        }
    }

    public async Task<MediaAsset?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.MediaAssets
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Database error occurred when retrieving media asset by id.");
            return null;
        }
    }

    public async Task UpdateAsync(MediaAsset mediaAsset, CancellationToken cancellationToken)
    {
        try
        {
            dbContext.Update(mediaAsset);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Database error occurred when updating media asset.");
        }
    }
}