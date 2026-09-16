using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Domain.Entities;
using FileService.Domain.MediaProcessing;
using Shared;

namespace FileService.Core.Processing;

public sealed class GetVideoProcessingStatusHandler
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IVideoProcessingRepository _videoProcessingRepository;

    public GetVideoProcessingStatusHandler(
        IMediaRepository mediaRepository,
        IVideoProcessingRepository videoProcessingRepository)
    {
        _mediaRepository = mediaRepository;
        _videoProcessingRepository = videoProcessingRepository;
    }

    public async Task<Result<VideoProcessingStatusResponse, Error>> Handle(
        Guid videoAssetId,
        CancellationToken cancellationToken)
    {
        Result<VideoAsset, Error> videoAssetResult = await _mediaRepository.GetVideoAssetSnapshotById(
            videoAssetId,
            cancellationToken);
        if (videoAssetResult.IsFailure)
            return videoAssetResult.Error;

        VideoProcess? videoProcess;
        Result<VideoProcess, Error> videoProcessResult = await _videoProcessingRepository.GetSnapshotByVideoAssetId(
            videoAssetId,
            cancellationToken);
        if (videoProcessResult.IsFailure)
            videoProcess = null;
        else
            videoProcess = videoProcessResult.Value;

        VideoProcessingStatusResponse videoProcessingStatusResponse = VideoProcessingStatusMapper
            .Map(videoAssetResult.Value, videoProcess);
        return videoProcessingStatusResponse;
    }
}