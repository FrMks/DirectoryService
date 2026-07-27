using CSharpFunctionalExtensions;
using FileService.Core.Multipart;
using FileService.Domain.Errors;
using FileService.Domain.MediaProcessing;
using FileService.VideoProcessing.FfmpegProcess;
using Microsoft.Extensions.Logging;
using Shared;

namespace FileService.VideoProcessing.Pipeline.Steps;

public class GenerateHlsStepHandler : IProcessingStepHandler
{
    private readonly IFfmpegProcessRunner _ffmpegProcessRunner;
    private readonly IS3Provider _s3Provider;
    private readonly ILogger<GenerateHlsStepHandler> _logger;

    public GenerateHlsStepHandler(
        IFfmpegProcessRunner ffmpegProcessRunner,
        IS3Provider s3Provider,
        ILogger<GenerateHlsStepHandler> logger)
    {
        _ffmpegProcessRunner = ffmpegProcessRunner;
        _s3Provider = s3Provider;
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

        // Url по которому ffmpeg может прочитать исходное загруженное видео
        string downloadFileUrl;
        if (!string.IsNullOrWhiteSpace(context.MediaAssetUrl))
        {
            downloadFileUrl = context.MediaAssetUrl;
        }
        else
        {
            _logger.LogDebug("Download file url not cached, generating new presigned URL");

            Result<string, Error> urlResult = await _s3Provider
                .GenerateDownloadUrlAsync(context.VideoAsset.UploadedKey!);
            if (urlResult.IsFailure)
                return urlResult.Error;

            downloadFileUrl = urlResult.Value;
            context.SetMediaAssetUrl(urlResult.Value);
        }

        if (string.IsNullOrEmpty(context.HlsOutputDirectory))
        {
            return FileError.HlsProcessingFailed();
        }

        if (context.VideoAsset.Metadata is null)
        {
            _logger.LogWarning("Metadata is null, progress tracking will be disabled");
        }

        UnitResult<Error> result = await _ffmpegProcessRunner.GenerateHlsAsync(
            downloadFileUrl,
            context.HlsOutputDirectory,
            cancellationToken);
        if (result.IsFailure)
            return result.Error;

        return context;
    }
}