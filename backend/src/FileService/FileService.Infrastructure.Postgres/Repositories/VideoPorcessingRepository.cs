using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FileService.Core;
using FileService.Domain.MediaProcessing;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace FileService.Infrastructure.Postgres.Repositories;

public class VideoPorcessingRepository : IVideoProcessingRepository
{
    private readonly FileServiceDbContext _dbContext;

    public VideoPorcessingRepository(FileServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(VideoProcess videoProcess)
    {
        _dbContext.VideoProcess.Add(videoProcess);
    }

    public async Task<Result<VideoProcess, Error>> GetBy(
        Expression<Func<VideoProcess, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        VideoProcess? videoProcess = await _dbContext.VideoProcess
            .Include(v => v.Steps)
            .FirstOrDefaultAsync(predicate, cancellationToken);

        if (videoProcess is null)
            return Error.NotFound("video.process.not.found", "Can not find video process in database");

        return videoProcess;
    }

    public async Task<Result<VideoProcess, Error>> GetSnapshotByVideoAssetId(
        Guid videoAssetId,
        CancellationToken cancellationToken = default)
    {
        VideoProcess? videoProcessResult = await _dbContext.VideoProcess.AsNoTracking()
            .Include(v => v.Steps)
            .FirstOrDefaultAsync(v => v.VideoAssetId == videoAssetId, cancellationToken);

        if (videoProcessResult is null)
            return Error.NotFound("video.process.not.found", "Can not find video process in database");

        return videoProcessResult;
    }
}