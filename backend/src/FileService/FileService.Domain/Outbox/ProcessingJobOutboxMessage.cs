namespace FileService.Domain.Outbox;

public sealed class ProcessingJobOutboxMessage
{
    private ProcessingJobOutboxMessage()
    {
    }

    public Guid Id { get; private set; }

    public Guid MediaAssetId { get; private set; }

    public ProcessingJobOutboxStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public int MaxRetries { get; private set; }

    public DateTimeOffset NextAttemptAt { get; private set; }

    public DateTimeOffset? StartedProcessingAt { get; private set; }

    public DateTimeOffset? LastAttemptedAt { get; private set; }

    public string? LastError { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public static ProcessingJobOutboxMessage Create(
        Guid mediaAssetId,
        int maxRetries)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        return new ProcessingJobOutboxMessage
        {
            Id = Guid.NewGuid(),
            MediaAssetId = mediaAssetId,
            Status = ProcessingJobOutboxStatus.Pending,
            Attempts = 0,
            MaxRetries = maxRetries,
            NextAttemptAt = now,
            CreatedAt = now,
        };
    }

    public void SwitchStatusTo(ProcessingJobOutboxStatus newStatus)
    {
        Status = newStatus;
    }

    public void IncrementAttempts()
    {
        Attempts += 1;
    }

    public void SetTimeWhenStartProcessing()
    {
        StartedProcessingAt = DateTimeOffset.UtcNow;
        LastAttemptedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCompleted()
    {
        Status = ProcessingJobOutboxStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        LastError = null;
    }

    public void MarkFailed(
        string error,
        int initialRetryDelaySeconds)
    {
        Status = ProcessingJobOutboxStatus.Failed;
        LastError = error;
        NextAttemptAt = CalculateNextAttemptAt(initialRetryDelaySeconds);
        LastAttemptedAt = DateTimeOffset.UtcNow;
    }

    public void ResetProcessingToPending()
    {
        Status = ProcessingJobOutboxStatus.Pending;
        StartedProcessingAt = null;
        NextAttemptAt = DateTimeOffset.UtcNow;
        LastAttemptedAt = DateTimeOffset.UtcNow;
    }

    private DateTimeOffset CalculateNextAttemptAt(int initialRetryDelaySeconds)
    {
        return DateTimeOffset.UtcNow.AddSeconds(initialRetryDelaySeconds * Math.Pow(2, Attempts - 1));
    }
}