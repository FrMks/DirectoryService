namespace FileService.Domain.IntegrationEvents;

public class ProcessedFileEvent
{
    public ProcessedFileEvent(Guid eventId, string eventType, DateTime processedAt)
    {
        EventId = eventId;
        EventType = eventType;
        ProcessedAt = processedAt;
    }

    public Guid EventId { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public DateTime ProcessedAt { get; private set; }
}