using System.Diagnostics;
using CSharpFunctionalExtensions;
using FileService.Domain.ValueObjects;
using FileService.VideoProcessing.ProcessRunner;
using Microsoft.Extensions.Options;
using Shared;

namespace FileService.VideoProcessing.FfmpegProcess;

// Я умею вызывать ffmpeg/ffprobe правильными командами
public class FfmpegProcessRunner : IFfmpegProcessRunner
{
    private readonly VideoProcessingOptions _options;
    private readonly IProcessRunner _processRunner;

    public FfmpegProcessRunner(
        IOptions<VideoProcessingOptions> options,
        IProcessRunner processRunner)
    {
        _options = options.Value;
        _processRunner = processRunner;
    }

    public async Task<Result<VideoMetadata, Error>> ExtractMetadataAsync(
        string inputFileUrl,
        CancellationToken ct = default)
    {
        string arguments = BuildFfprobeArguments(inputFileUrl);
        var command = new ProcessCommand(_options.FfprobePath, arguments);

        Result<ProcessResult, Error> processResult = await _processRunner.RunAsync(
            command,
            cancellationToken: ct);
        if (processResult.IsFailure)
            return processResult.Error;

        return FfprobeOutputParser.Parse(processResult.Value.StandardOutput);
    }

    private static string BuildFfprobeArguments(string inputFileUrl)
    {
        return
            $"""
            -v error
            -select_streams v:0
            -show_entries stream=width,height,codex_name:format=duration,format_name
            -of json
            "{inputFileUrl}"
            """;
    }
}
