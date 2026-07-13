using CSharpFunctionalExtensions;
using FileService.Domain.Entities;
using FileService.Domain.MediaProcessing;
using FileService.Domain.ValueObjects;
using Shared;

namespace FileService.VideoProcessing;

public class ProcessingPipeline : IProcessingPipeline
{
    public async Task<UnitResult<Error>> ProcessAllStepsAsync(
        Guid videoAssetId,
        CancellationToken cancellationToken = default)
    {
        return UnitResult.Success<Error>();
    }
}

public interface IProcessingStepHandler
{
    StepType StepType { get; }

    Task<Result<ProcessingContext, Error>> ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken = default);
}

public sealed class ProcessingContext
{
    public VideoProcess VideoPorcess { get; init; }

    public VideoAsset VideoAsset { get; init; }

    public string? WorkingDirectory { get; private set; }

    // где будут генерироваться в нашей файловой системе hls файлы
    public string? HlsOutputDirectory { get; private set; }
}