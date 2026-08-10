using FileService.Core.Processing;
using FileService.Domain.Entities;
using FileService.Domain.Entities.MediaAssetEntity;
using Quartz;

namespace FileService.VideoProcessing.Jobs;

public class VideoProcessingJobFactory : IProcessingJobFactory
{
    private const string JobGroup = "video-processing";

    public bool CanProcess(MediaAsset mediaAsset)
    {
        return mediaAsset is VideoAsset;
    }

    public IJobDetail CreateJob(MediaAsset mediaAsset)
    {
        return JobBuilder.Create<VideoProcessingJob>()
            .WithIdentity($"video-processing-{mediaAsset.Id}", JobGroup)
            .UsingJobData(VideoProcessingJob.VideoAssetIdKey.Name, mediaAsset.Id.ToString())
            .StoreDurably(false)
            .Build();
    }

    public ITrigger CreateTrigger(MediaAsset mediaAsset)
    {
        return TriggerBuilder.Create()
            .WithIdentity($"video-processing-trigger-{mediaAsset.Id}", JobGroup)
            .StartNow()
            .Build();
    }
}