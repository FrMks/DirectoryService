using Quartz;

namespace FileService.VideoProcessing.Jobs;

public sealed class ProcessingRetryScheduler : FileService.Core.Processing.IProcessingRetryScheduler
{
    private const string JobGroup = "video-processing";

    private readonly ISchedulerFactory _schedulerFactory;

    public ProcessingRetryScheduler(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    public async Task ScheduleAsync(
        Guid videoAssetId,
        int retryCount,
        DateTime nextRetryAt,
        CancellationToken cancellationToken = default)
    {
        // Запусти существующий job, который запускает VideoProcessingJob, еще раз в другое время
        // Если бы мы создали еще один джоб, то могло произойти бы параллельная обработка видео
        JobKey jobKey = new($"video-processing-{videoAssetId}", JobGroup);
        TriggerKey triggerKey = new(
            $"video-processing-retry-{videoAssetId}-{retryCount}",
            JobGroup);

        ITrigger retryTrigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob(jobKey)
            .StartAt(nextRetryAt)
            .Build();

        IScheduler scheduler = await _schedulerFactory
            .GetScheduler(cancellationToken);

        // Так как JobDetail уже сохранен, создается только триггер.
        await scheduler.ScheduleJob(retryTrigger, cancellationToken);
    }
}
