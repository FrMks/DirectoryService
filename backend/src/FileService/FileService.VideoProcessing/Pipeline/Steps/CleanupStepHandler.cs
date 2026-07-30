using CSharpFunctionalExtensions;
using FileService.Core.Multipart;
using FileService.Domain.MediaProcessing;
using Microsoft.Extensions.Logging;
using Shared;

namespace FileService.VideoProcessing.Pipeline.Steps;

public class CleanupStepHandler : IProcessingStepHandler
{
    private readonly ILogger<CleanupStepHandler> _logger;
    private readonly IS3Provider _s3Provider;

    public CleanupStepHandler(ILogger<CleanupStepHandler> logger, IS3Provider s3Provider)
    {
        _logger = logger;
        _s3Provider = s3Provider;
    }

    public StepType StepType => StepType.CLEANUP;

    public Task<Result<ProcessingContext, Error>> ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken = default)
    {

    }
}
