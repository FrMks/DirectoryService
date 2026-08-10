using FileService.Domain.Entities.MediaAssetEntity;
using Quartz;

namespace FileService.Core.Processing;

public interface IProcessingJobFactory
{
    bool CanProcess(MediaAsset mediaAsset);

    IJobDetail CreateJob(MediaAsset mediaAsset);

    ITrigger CreateTrigger(MediaAsset mediaAsset);
}