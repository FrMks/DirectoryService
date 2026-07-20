using System.Diagnostics;
using CSharpFunctionalExtensions;
using FileService.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using Shared;

namespace FileService.VideoProcessing.FfmpegProcess;

public class FfmpegProcessRunner : IFfmpegProcessRunner
{
    private readonly VideoProcessingOptions _options;

    public FfmpegProcessRunner(IOptions<VideoProcessingOptions> options)
    {
        _options = options.Value;
    }

    public Task<Result<VideoMetadata, Error>> ExtractMetadataAsync(
        string inputFileUrl,
        CancellationToken ct = default)
    {

    }
}
