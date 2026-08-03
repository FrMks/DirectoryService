using CSharpFunctionalExtensions;
using FileService.Core;
using FileService.Domain.Entities;
using FileService.Domain.Enums;
using FileService.Domain.MediaProcessing;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Core.Database;
using Shared.Framework.EndpointResults;

namespace FileService.VideoProcessing.Pipeline;

public class ProcessingPipeline : IProcessingPipeline
{
    private const string PipelineCancelledCode = "processing.pipeline.cancelled";

    private readonly IEnumerable<IProcessingStepHandler> _stepHandlers;

    private readonly ILogger<ProcessingPipeline> _logger;

    private readonly IVideoProcessingRepository _videoProcessingRepository;

    private readonly IMediaRepository _mediaAssetRepository;

    private readonly ITransactionManager _transactionManager;

    public ProcessingPipeline(
        IEnumerable<IProcessingStepHandler> stepHandlers,
        ILogger<ProcessingPipeline> logger,
        IMediaRepository mediaAssetRepository,
        IVideoProcessingRepository videoProcessingRepository,
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
        CancellationToken contextCancellationToken = cancellationToken.IsCancellationRequested
            ? CancellationToken.None
            : cancellationToken;

        Result<ProcessingContext, Error> contextResult = await LoadContextAsync(videoAssetId, contextCancellationToken);
        if (contextResult.IsFailure)
            return contextResult.Error;

        ProcessingContext context = contextResult.Value;

        UnitResult<Error> executionResult = await ExecuteAllStepsAsync(context, cancellationToken);
        if (executionResult.IsFailure)
        {
            CancellationToken failureCancellationToken = executionResult.Error.Code == PipelineCancelledCode
                ? CancellationToken.None
                : cancellationToken;

            return await FinalizeWithFailureAsync(context, executionResult.Error, failureCancellationToken);
        }

        return await FinalizeAsync(context, cancellationToken);
    }

    private async Task<UnitResult<Error>> FinalizeWithFailureAsync(
        ProcessingContext context,
        Error error,
        CancellationToken cancellationToken)
    {
        Guid videoAssetId = context.VideoAsset.Id;

        context.VideoProcess.Fail(error.Message);
        if (context.VideoAsset.Status != MediaStatus.FAILED)
            context.VideoAsset.FailProcessing(DateTime.UtcNow);

        _logger.LogError(
            "Video processing failed for VideoAssetId: {VideoAssetId}. Error: {Error}",
            videoAssetId,
            error.Message);

        CleanupWorkingDirectory(context);

        UnitResult<Error> saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            _logger.LogError(
                "Failed to save failure state for VideoAssetId: {VideoAssetId}",
                videoAssetId);
            return saveResult.Error;
        }

        return UnitResult.Failure(error);
    }

    private void CleanupWorkingDirectory(ProcessingContext context)
    {
        if (string.IsNullOrWhiteSpace(context.WorkingDirectory))
            return;

        try
        {
            if (Directory.Exists(context.WorkingDirectory))
            {
                Directory.Delete(context.WorkingDirectory, recursive: true);
                _logger.LogDebug(
                    "Working directory deleted: {WorkingDirectory}",
                    context.WorkingDirectory);
            }

            context.Cleanup();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to delete working directory: {WorkingDirectory} for VideoAssetId: {VideoAssetId}",
                context.WorkingDirectory,
                context.VideoAsset.Id);
        }
    }

    private async Task<UnitResult<Error>> FinalizeAsync(
        ProcessingContext context,
        CancellationToken cancellationToken)
    {
        Guid videoAssetId = context.VideoAsset.Id;

        UnitResult<Error> completeVideoResult = context.VideoAsset.CompleteProcessing(DateTime.UtcNow);
        if (completeVideoResult.IsFailure)
            return completeVideoResult.Error;

        if (context.VideoProcess.Status != ProcessingStatus.COMPLETED)
        {
            UnitResult<Error> completeProcessResult = context.VideoProcess.Complete();
            if (completeProcessResult.IsFailure)
                return completeProcessResult.Error;
        }

        _logger.LogInformation(
            "Video processing completed successfully for VideoAssetId: {VideoAssetId}",
            videoAssetId);

        UnitResult<Error> saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            _logger.LogError(
                "Failed to save final state for VideoAssetId: {VideoAssetId}",
                videoAssetId);
            return saveResult.Error;
        }

        return UnitResult.Success<Error>();
    }

    private async Task<UnitResult<Error>> ExecuteAllStepsAsync(
        ProcessingContext context,
        CancellationToken cancellationToken)
    {
        Guid videoAssetId = context.VideoAsset.Id;

        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "Processing pipeline was cancelled for VideoAssetId: {VideoAssetId}",
                    videoAssetId);

                return Error.Failure(PipelineCancelledCode, "Video processing was cancelled");
            }

            // Получаем активный шаг или запускаем следующий ожидающий этап обрабокти или null.
            Result<ProcessingStep?, Error> stepResult = context.VideoProcess.ProcessNextStep();

            if (stepResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to process next step for VideoAssetId: {VideoAssetId}. Status: {Status}",
                    videoAssetId,
                    context.VideoProcess.Status);
                return stepResult.Error;
            }

            if (stepResult.Value is null)
            {
                _logger.LogInformation(
                    "All processing steps executed for VideoAssetId: {VideoAssetId}",
                    videoAssetId);
                return UnitResult.Success<Error>();
            }

            ProcessingStep currentStep = stepResult.Value;

            _logger.LogInformation(
                "Processing step {StepType} (Order: {Order}) for VideoAssetId: {VideoAssetId}",
                currentStep.StepType,
                currentStep.Order,
                videoAssetId);

            IProcessingStepHandler? stepHandler = _stepHandlers.FirstOrDefault(s => s.StepType == currentStep.StepType);
            if (stepHandler is null)
            {
                string error = $"No handler found for step type {currentStep.StepType}";
                _logger.LogError("No handler fount for step type {StepType}", currentStep.StepType);

                context.VideoProcess.FailCurrentStep(error);
                context.VideoProcess.Fail(error, isCritical: true);
                context.VideoAsset.FailProcessing(DateTime.UtcNow);

                UnitResult<Error> saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
                if (saveResult.IsFailure)
                {
                    _logger.LogError(
                        "Failed to save context after missing handler for step {StepType} for VideoAssetId: {VideoAssetId}",
                        currentStep.StepType,
                        videoAssetId);
                }

                return Error.Failure("pipeline.handler.not.found", error);
            }

            Result<ProcessingContext, Error> executionResult = await ExecuteStepSafelyAsync(
                stepHandler,
                context,
                cancellationToken);

            if (executionResult.IsFailure)
            {
                _logger.LogError(
                    "Step {StepType} failed for VideoAssetId: {VideoAssetId}. Error: {Error}",
                    currentStep.StepType,
                    videoAssetId,
                    executionResult.Error);

                context.VideoProcess.FailCurrentStep(executionResult.Error.Message);
                context.VideoProcess.Fail(executionResult.Error.Message, isCritical: true);
                context.VideoAsset.FailProcessing(DateTime.UtcNow);

                UnitResult<Error> saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
                if (saveResult.IsFailure)
                {
                    _logger.LogError(
                        "Failed to save context after step failure {StepType} for VideoAssetId: {VideoAssetId}",
                        currentStep.StepType,
                        videoAssetId);
                }

                return executionResult.Error;
            }

            context.VideoProcess.CompleteCurrentStep();

            _logger.LogInformation(
                "Step {StepType} completed for VideoAssetId: {VideoAssetId}. Progress: {Progress}%",
                currentStep.StepType,
                videoAssetId,
                context.VideoProcess.ProgressPercentage);

            UnitResult<Error> completeSaveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
            if (completeSaveResult.IsFailure)
            {
                _logger.LogError(
                    "Failed to save context after step {StepType} for VideoAssetId: {VideoAssetId}",
                    currentStep.StepType,
                    videoAssetId);
                return completeSaveResult.Error;
            }
        }
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

    private async Task<Result<ProcessingContext, Error>> ExecuteStepSafelyAsync(
        IProcessingStepHandler stepHandler,
        ProcessingContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            return await stepHandler.ExecuteAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception in step handler {StepType} for VideoAssetId: {VideoAssetId}",
                stepHandler.StepType,
                context.VideoAsset.Id);

            return Error.Failure("pipeline.step.exception", $"Step execution failed: {ex.Message}");
        }
    }
}
