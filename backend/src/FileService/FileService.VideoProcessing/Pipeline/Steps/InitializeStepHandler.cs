using CSharpFunctionalExtensions;
using FileService.Domain.MediaProcessing;
using Microsoft.Extensions.Logging;
using Shared;

namespace FileService.VideoProcessing.Pipeline.Steps;

public class InitializeStepHandler : IProcessingStepHandler
{
    public StepType StepType => StepType.INITIALIZE;

    private readonly ILogger<InitializeStepHandler> _logger;

    public InitializeStepHandler(ILogger<InitializeStepHandler> logger)
    {
        _logger = logger;
    }

    public Task<Result<ProcessingContext, Error>> ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken = default)
    {
        // Создать дирректорию, в которой будет происходить работа в файловой системе
        _logger.LogInformation(
            "Initializing video processing for VideoAssetId: {VideoAssetId}",
            context.VideoAsset.Id);

        UnitResult<Error> createResult = context.CreateWorkingDirectory();
        if (createResult.IsFailure)
            return Task.FromResult(Result.Failure<ProcessingContext, Error>(createResult.Error));

        _logger.LogDebug(
            "Working directory created: {WorkingDirectory}",
            context.WorkingDirectory);

        return Task.FromResult(Result.Success<ProcessingContext, Error>(context));
    }
}