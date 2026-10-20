using CSharpFunctionalExtensions;
using FileService.Contracts.IntegrationEvents;
using Microsoft.Extensions.Logging;
using Shared;

namespace FileService.Core.Event;

public class FileDeletedConsumer
{
    private readonly IProcessedFileEventRepository _processedFileEventRepository;
    private readonly ILogger<FileDeletedConsumer> _logger;

    public FileDeletedConsumer(
        IProcessedFileEventRepository processedFileEventRepository,
        ILogger<FileDeletedConsumer> logger)
    {
        _processedFileEventRepository = processedFileEventRepository;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> Handle(FileDeleted message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received FileDeleted event {EventId} for file {FileId}",
            message.EventId,
            message.FileId);

        bool isNewEvent = await _processedFileEventRepository.TryRegisterAsync(
            message.EventId,
            nameof(FileDeleted),
            cancellationToken);

        if (!isNewEvent) // Если дубликат
        {
            return Result.Success<Error>();
        }

        _logger.LogInformation(
            "Registered new event with fileId: {FileId}, eventId: {EventId}, targetEntityType: {TargetEntityType}",
            message.FileId,
            message.EventId,
            message.TargetEntityType);

        return Result.Success<Error>();
    }
}