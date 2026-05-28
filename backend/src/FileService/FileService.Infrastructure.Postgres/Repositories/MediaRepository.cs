using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FileService.Core;
using FileService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace FileService.Infrastructure.Postgres.Repositories;

public class MediaRepository(FileServiceDbContext dbContext, ILogger<MediaRepository> logger) : IMediaRepository
{
    public async Task<Result<Guid, Error>> AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.MediaAssets.AddAsync(mediaAsset, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully added to the database with id{mediaAsset}", mediaAsset.Id);
            return mediaAsset.Id;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Database error occurred when added media asset to a database.");
            return Error.Failure("database.error", "Error when added media asset to a database.");
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

    public async Task<Result<MediaAsset, Error>> GetBy(
        Expression<Func<MediaAsset, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        MediaAsset? mediaAsset = await dbContext.MediaAssets.FirstOrDefaultAsync(predicate, cancellationToken);
        if (mediaAsset is null)
            return Error.NotFound(null, "media file");

        return mediaAsset;
    }

    public async Task<int> SaveAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken);
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