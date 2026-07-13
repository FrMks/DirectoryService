using FileService.Core;
using CSharpFunctionalExtensions;
using FileService.Domain.MediaProcessing;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Core.Database;
using FileService.Domain.Entities.MediaAssetEntity;
using FileService.Domain.Entities;

namespace FileService.VideoProcessing.Pipeline;

public class ProcessingPipeline : IProcessingPipeline
{
    private readonly IEnumerable<IProcessingStepHandler> _stepHandlers;

    private readonly ILogger<ProcessingPipeline> _logger;

    private readonly IVideoProcessingRepository _videoProcessingRepository;

    private readonly IMediaRepository _mediaAssetRepository;

    private readonly ITransactionManager _transactionManager;

    public ProcessingPipeline(
        IEnumerable<IProcessingStepHandler> stepHandlers,
        ILogger<ProcessingPipeline> logger,
        IMediaRepository mediaAssetRepository,
        IVideoProcessingRepository videoProcessingRepository
        ITransactionManager transactionManager)
    {
        _stepHandlers = stepHandlers;
        _logger = logger;
        _mediaAssetRepository = mediaAssetRepository;
        _videoProcessingRepository = videoProcessingRepository;
        _transactionManager = transactionManager;
    }

    public async Task<UnitResult<Error>> ProcessAllStepsAsync(
        Guid videoAssetId,
        CancellationToken cancellationToken = default)
    {
        Result<ProcessingContext, Error> contextResult = await LoadContextAsync(videoAssetId, cancellationToken);
        if (contextResult.IsFailure)
            return contextResult.Error;

        return UnitResult.Success<Error>();
    }

    // - ProcessingContext - какое видео обрабатываем, какой VideoProcess отвечает за lifecycle шагов,
    //      где лежит raw input, куда писать HLS output, где временная рабочая директория,
    //      какая metadata уже извлечена, какой progress
    // - VideoProcess - status процесса, текущий step, список steps, progress, ошибка
    // - VideoAsset - это само видео, как media asset
    private async Task<Result<ProcessingContext, Error>> LoadContextAsync(
        Guid videoAssetId,
        CancellationToken cancellationToken)
    {
        Result<VideoProcess, Error> processingResult = await _videoProcessingRepository
            .GetBy(vp => vp.VideoAssetId == videoAssetId, cancellationToken);

        // pipeline должен иметь доменную модель процесса и список шагов
        VideoProcess videoProcess;

        if (processingResult.IsFailure)
        {
            VideoProcess newPorcess = new(videoAssetId);
            videoProcess = newPorcess;

            _videoProcessingRepository.Add(videoProcess);

            _logger.LogInformation("Created new VideoProcessing for VideoAssetId: {VideoAsssetId}", videoAssetId);
        }
        else
        {
            videoProcess = processingResult.Value;
            _logger.LogInformation("Loaded existing VideoProcessing for VideoAssetId: {VideoAssetId}", videoAssetId);
        }

        // Video asset наследуется от Media asset. То есть VideoAsset.Id тот же самый MediaAsset.Id
        Result<VideoAsset, Error> assetResult = await _mediaAssetRepository
            .GetVideoAssetBy(va => va.Id == videoAssetId, cancellationToken);

        if (assetResult.IsFailure)
        {
            _logger.LogError("An Error in occurred when we try to get media asset by mediaAsset.Id == videoAssetId");
            return assetResult.Error;
        }

        UnitResult<Error> startResult = assetResult.Value.StartProcessing();
        if (startResult.IsFailure)
            return startResult.Error;

        UnitResult<Error> saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error;

        ProcessingContext processingContext = new ProcessingContext
        {
            VideoAsset = assetResult.Value,
            VideoProcess = videoProcess,
        };

        return processingContext;

    }
}