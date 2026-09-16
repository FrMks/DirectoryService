using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FileService.Core;
using FileService.Domain.Entities;
using FileService.Domain.Entities.MediaAssetEntity;
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

    public void Add(MediaAsset mediaAsset)
    {
        dbContext.MediaAssets.Add(mediaAsset);
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

    public async Task<Result<VideoAsset, Error>> GetVideoAssetBy(
        Expression<Func<VideoAsset, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        VideoAsset? videoAsset = await dbContext.VideoAssets
            .OfType<VideoAsset>()
            .FirstOrDefaultAsync(predicate, cancellationToken);
        if (videoAsset is null)
            return Error.NotFound(null, "media file");

        return videoAsset;
    }

    public async Task<Result<IReadOnlyList<MediaAsset>, Error>> GetManyBy(
        Expression<Func<MediaAsset, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<MediaAsset> mediaAssets = await dbContext.MediaAssets
            .Where(predicate)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<MediaAsset>, Error>(mediaAssets);
    }

    public async Task<int> SaveAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result<VideoAsset, Error>> GetVideoAssetSnapshotById(
        Guid videoAssetId,
        CancellationToken cancellationToken)
    {
        VideoAsset? videoAssetResult = await dbContext.VideoAssets.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == videoAssetId, cancellationToken);

        if (videoAssetResult is null)
            return Error.NotFound("video.asset.not.found", "Can not find video asset in database");

        return videoAssetResult;
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