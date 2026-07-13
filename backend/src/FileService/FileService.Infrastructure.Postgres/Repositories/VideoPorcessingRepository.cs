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

    public async Task Add(VideoProcess videoProcess, CancellationToken cancellationToken = default)
    {
        await _dbContext.VideoProcess.AddAsync(videoProcess, cancellationToken);
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
}