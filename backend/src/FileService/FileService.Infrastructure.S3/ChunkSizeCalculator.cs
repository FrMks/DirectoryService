using CSharpFunctionalExtensions;
using FileService.Infrastructure.S3;
using Microsoft.Extensions.Options;
using Shared;

namespace FileService.Core.Files;

public class ChunkSizeCalculator : IChunkSizeCalculator
{
    private readonly S3Options _options;

    public ChunkSizeCalculator(IOptions<S3Options> options)
    {
        _options = options.Value;
    }

    public Result<(long ChunkSize, int TotalChunks), Error> CalculateChunkSize(
        long fileSize,
        long recommendedChunkSizeBytes,
        int maxChunks)
    {
        if (recommendedChunkSizeBytes <= 0 || maxChunks <= 0)
            return Error.Validation("setting.of.chunks", "Setting of chunks");

        if (fileSize <= recommendedChunkSizeBytes)
            return (fileSize, 1);

        int calculatedChunks = (int)Math.Ceiling((double)fileSize / recommendedChunkSizeBytes);

        int actualChunks = Math.Min(calculatedChunks, maxChunks);

        long chunkSize = fileSize / actualChunks;

        return (chunkSize, actualChunks);
    }
}