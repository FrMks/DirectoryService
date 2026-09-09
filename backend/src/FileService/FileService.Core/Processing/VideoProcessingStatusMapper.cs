using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Domain.Entities;
using FileService.Domain.Enums;
using FileService.Domain.MediaProcessing;
using Shared;

namespace FileService.Core.Processing;

public class VideoProcessingStatusMapper
{
    private const string Queued = "queued";
    private const string Processing = "processing";
    private const string Ready = "ready";
    private const string Failed = "failed";
    private const string Deleted = "deleted";

    public static Result<VideoProcessingStatusResponse, Error> Mapper(
        VideoAsset videoAsset,
        VideoProcess? videoProcess)
    {
        string status = MapStatus(videoAsset);
        string? currentStep = MapStepType(videoProcess, status);
        int percent = MapPercent(videoProcess, status);

        VideoProcessingStatusResponse response = new(
            AssetId: videoAsset.Id,
            Status: status,
            CurrentStep: currentStep,
            Percent: percent,
            ErrorCode: videoProcess?.ErrorMessage);
        return Result.Success<VideoProcessingStatusResponse, Error>(response);
    }

    private static string MapStatus(VideoAsset videoAsset)
    {
        switch (videoAsset.Status)
        {
            case MediaStatus.UPLOADING:
            case MediaStatus.UPLOADED:
            case MediaStatus.PENDING_PROCESSING:
                return Queued;
            case MediaStatus.PROCESSING:
                return Processing;
            case MediaStatus.READY:
                return Ready;
            case MediaStatus.FAILED:
                return Failed;
            case MediaStatus.DELETED:
                return Deleted;
            default:
                throw new ArgumentOutOfRangeException(nameof(videoAsset.Status), $"Unexpected status value: {videoAsset.Status}");
        }
    }

    private static string? MapStepType(VideoProcess? videoProcess, string status)
    {
        StepType? stepType = videoProcess?.CurrentStep?.StepType;
        if (stepType is null)
            return null;

        if (status == Processing)
            return stepType?.ToString().ToLowerInvariant();

        return null;
    }

    private static int MapPercent(VideoProcess? videoProcess, string status)
    {
        if (status == Queued)
            return 0;

        if (status == Ready)
            return 100;

        if (videoProcess is null)
            return 0;

        return Math.Clamp(videoProcess.ProgressPercentage, 0, 100);
    }
}