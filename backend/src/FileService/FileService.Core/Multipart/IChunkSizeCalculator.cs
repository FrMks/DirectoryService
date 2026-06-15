using CSharpFunctionalExtensions;
using Shared;

namespace FileService.Core.Files;

public interface IChunkSizeCalculator
{
    Result<(long ChunkSize, int TotalChunks), Error> CalculateChunkSize(long fileSize);
}