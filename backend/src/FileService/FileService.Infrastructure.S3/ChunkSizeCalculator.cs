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

    public Result<(long ChunkSize, int TotalChunks), Error> CalculateChunkSize(long fileSize)
    {
        if (_options.RecommendedChunkSizeBytes <= 0 || _options.MaxChunks <= 0)
            return Error.Validation("setting.of.chunks", "Setting of chunks");

        if (fileSize <= _options.RecommendedChunkSizeBytes)
            return (fileSize, 1);

        int calculatedChunks = (int)Math.Ceiling((double)fileSize / _options.RecommendedChunkSizeBytes);

        int actualChunks = Math.Min(calculatedChunks, _options.MaxChunks);

        long chunkSize = (fileSize + actualChunks - 1) / actualChunks;

        return (chunkSize, actualChunks);
    }
}
