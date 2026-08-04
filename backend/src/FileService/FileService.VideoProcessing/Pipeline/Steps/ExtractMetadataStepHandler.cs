using CSharpFunctionalExtensions;
using FileService.Domain.Errors;
using FileService.Domain.MediaProcessing;
using FileService.Domain.ValueObjects;
using FileService.VideoProcessing.FfmpegProcess;
using Microsoft.Extensions.Logging;
using Shared;

namespace FileService.VideoProcessing.Pipeline.Steps;

public class ExtractMetadataStepHandler : IProcessingStepHandler
{
    private readonly ILogger<ExtractMetadataStepHandler> _logger;
    private readonly IFfmpegProcessRunner _ffmpegProcessRunner;

    public ExtractMetadataStepHandler(
        ILogger<ExtractMetadataStepHandler> logger,
        IFfmpegProcessRunner ffmpegProcessRunner)
    {
        _logger = logger;
        _ffmpegProcessRunner = ffmpegProcessRunner;
    }

    public StepType StepType => StepType.EXTRACT_METADATA;

    public async Task<Result<ProcessingContext, Error>> ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Extract metadata for VideoAssetId: {VideoAssetId} start",
            context.VideoAsset.Id);

        if (string.IsNullOrWhiteSpace(context.SourceFilePath))
            return Error.Failure("source.file.missing", "Source file path is not set in context");

        Result<VideoMetadata, Error> metadataResult = await _ffmpegProcessRunner.ExtractMetadataAsync(
            context.SourceFilePath,
            cancellationToken);
        if (metadataResult.IsFailure)
            return metadataResult.Error;

        UnitResult<Error> setMetadataResult = context.VideoAsset.SetMetadata(metadataResult.Value);
        if (setMetadataResult.IsFailure)
            return setMetadataResult.Error;

        _logger.LogInformation(
            "Extract metadata for VideoAssetId: {VideoAssetId} finished",
            context.VideoAsset.Id);

        return context;
    }
}
