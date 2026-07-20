using CSharpFunctionalExtensions;
using FileService.Domain.ValueObjects;
using Shared;

namespace FileService.VideoProcessing.FfmpegProcess;

public interface IFfmpegProcessRunner
{
    Task<Result<VideoMetadata, Error>> ExtractMetadataAsync(
        string inputFileUrl,
        CancellationToken ct = default);
}
