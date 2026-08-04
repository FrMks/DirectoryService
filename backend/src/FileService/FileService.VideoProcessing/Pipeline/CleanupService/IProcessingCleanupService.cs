using CSharpFunctionalExtensions;
using Shared;

namespace FileService.VideoProcessing.Pipeline.CleanupService;

public interface IProcessingCleanupService
{
    Task<UnitResult<Error>> CleanupUploadedSourceAsync(ProcessingContext context, CancellationToken cancellationToken = default);

    UnitResult<Error> CleanupWorkingDirectory(ProcessingContext context);
}