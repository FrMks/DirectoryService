using CSharpFunctionalExtensions;
using FileService.Domain.Errors;
using FileService.Domain.MediaProcessing;
using FileService.VideoProcessing.FfmpegProcess;
using Microsoft.Extensions.Logging;
using Shared;

namespace FileService.VideoProcessing.Pipeline.Steps;

public class GenerateHlsStepHandler : IProcessingStepHandler
{
    private readonly IFfmpegProcessRunner _ffmpegProcessRunner;
    private readonly ILogger<GenerateHlsStepHandler> _logger;

    public GenerateHlsStepHandler(
        IFfmpegProcessRunner ffmpegProcessRunner,
        ILogger<GenerateHlsStepHandler> logger)
    {
        _ffmpegProcessRunner = ffmpegProcessRunner;
        _logger = logger;
    }

    public StepType StepType => StepType.GENERATE_HLS;

    public async Task<Result<ProcessingContext, Error>> ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating HLS for VideoAssetId: {VideoAssetId}",
            context.VideoAsset.Id);

        if (string.IsNullOrWhiteSpace(context.SourceFilePath))
            return FileError.ObjectNotFound("source file path");

        if (string.IsNullOrEmpty(context.HlsOutputDirectory))
        {
            return FileError.HlsProcessingFailed();
        }

        if (context.VideoAsset.Metadata is null)
        {
            _logger.LogWarning("Metadata is null, progress tracking will be disabled");
        }

        UnitResult<Error> result = await _ffmpegProcessRunner.GenerateHlsAsync(
            context.SourceFilePath,
            context.HlsOutputDirectory,
            cancellationToken);
        if (result.IsFailure)
            return result.Error;

        return context;
    }
}
