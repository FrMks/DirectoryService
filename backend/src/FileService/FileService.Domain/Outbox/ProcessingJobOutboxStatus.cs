namespace FileService.Domain.Outbox;

public enum ProcessingJobOutboxStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
}