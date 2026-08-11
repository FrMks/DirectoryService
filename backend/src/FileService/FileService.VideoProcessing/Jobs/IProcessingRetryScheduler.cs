namespace FileService.VideoProcessing.Jobs;

public interface IProcessingRetryScheduler
{
    Task ScheduleAsync(
        Guid videoAssetId,
        int retryCount,
        DateTime nextRetryAt,
        CancellationToken cancellationToken = default);
}
