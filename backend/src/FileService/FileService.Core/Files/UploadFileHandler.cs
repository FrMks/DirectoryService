using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Core.Abstractions;
using Shared.Core.Extensions;

namespace FileService.Core.Files;

public class UploadFileHandler(
    IFileStorage fileStorage,
    ILogger<UploadFileHandler> logger)
    : ICommandHandler<string, UploadFileCommand>
{
    public async Task<Result<string, Errors>> Handle(
        UploadFileCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            await fileStorage.UploadFileAsync(
                command.Stream,
                command.BucketName,
                command.Key,
                command.ContentType,
                cancellationToken);

            return command.Key;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to upload file with key {Key}", command.Key);

            return Error.Failure(null, "Failed to upload file.").ToErrors();
        }
    }
}
