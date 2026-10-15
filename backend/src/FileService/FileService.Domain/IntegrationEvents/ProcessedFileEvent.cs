namespace FileService.Domain.IntegrationEvents;

public class ProcessedFileEvent
{
    public Guid EventId { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public DateTime ProcessedAt { get; private set; }
}