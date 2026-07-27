using CSharpFunctionalExtensions;
using FileService.Domain.ValueObjects;
using Shared;

namespace FileService.VideoProcessing.FfmpegProcess;

public interface IFfmpegProcessRunner
{
    Task<Result<VideoMetadata, Error>> ExtractMetadataAsync(
        string inputFileUrl,
        CancellationToken ct = default);

    Task<UnitResult<Error>> GenerateHlsAsync(
        string inputFileUrl,
        string outputDirectory,
        CancellationToken cancellationToken = default);
}
