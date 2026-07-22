using CSharpFunctionalExtensions;
using FileService.Core.Multipart;
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
    private readonly IS3Provider _s3Provider;

    public ExtractMetadataStepHandler(
        ILogger<ExtractMetadataStepHandler> logger,
        IFfmpegProcessRunner ffmpegProcessRunner,
        IS3Provider s3Provider)
    {
        _logger = logger;
        _ffmpegProcessRunner = ffmpegProcessRunner;
        _s3Provider = s3Provider;
    }

    public StepType StepType => StepType.EXTRACT_METADATA;

    public async Task<Result<ProcessingContext, Error>> ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Extract metadata for VideoAssetId: {VideoAssetId} start",
            context.VideoAsset.Id);

        StorageKey? uploadKey = context.VideoAsset.UploadedKey;
        if (uploadKey is null)
            return FileError.ObjectNotFound();

        Result<string, Error> downloadUrlResult = await _s3Provider
            .GenerateDownloadUrlAsync(uploadKey);
        if (downloadUrlResult.IsFailure)
            return downloadUrlResult.Error;

        Result<VideoMetadata, Error> metadataResult = await _ffmpegProcessRunner.ExtractMetadataAsync(
            downloadUrlResult.Value,
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
