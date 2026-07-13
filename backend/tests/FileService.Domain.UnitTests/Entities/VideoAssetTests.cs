using FileService.Domain.Entities;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using FluentAssertions;

namespace FileService.Domain.UnitTests.Entities;

public sealed class VideoAssetTests
{
    [Fact]
    public void CreateForUpload_ShouldCreateVideoInUploadingStateWithPlannedHlsResult()
    {
        VideoAsset video = CreateVideoAsset();

        video.Id.Should().NotBeEmpty();
        video.Status.Should().Be(MediaStatus.UPLOADING);
        video.RequiresProcessing().Should().BeTrue();
        video.RawKey.FullPath.Should().StartWith("videos/raw/");
        video.HlsRootKey.FullPath.Should().StartWith("videos/hls/");
        video.HlsResult.ManifestKey.FullPath.Should().Be($"{video.HlsRootKey.FullPath}/master.m3u8");
        video.FinalKey.Should().Be(StorageKey.None);
        video.UploadedKey.Should().Be(video.RawKey);
    }

    [Fact]
    public void VideoLifecycle_ShouldMoveThroughValidProcessingTransitions()
    {
        VideoAsset video = CreateVideoAsset();

        video.MarkUploaded(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        video.Status.Should().Be(MediaStatus.UPLOADED);

        video.MarkPendingProcessing(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        video.Status.Should().Be(MediaStatus.PENDING_PROCESSING);

        video.StartProcessing(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        video.Status.Should().Be(MediaStatus.PROCESSING);

        video.CompleteProcessing(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        video.Status.Should().Be(MediaStatus.READY);
        video.FinalKey.Should().Be(video.HlsResult.ManifestKey);
    }

    [Fact]
    public void MarkReady_ShouldFail_WhenVideoWasOnlyUploaded()
    {
        VideoAsset video = CreateUploadedVideoAsset();

        var result = video.MarkReady(video.HlsResult.ManifestKey, DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        video.Status.Should().Be(MediaStatus.UPLOADED);
        video.FinalKey.Should().Be(StorageKey.None);
    }

    [Fact]
    public void CompleteProcessing_ShouldFail_WhenProcessingWasNotStarted()
    {
        VideoAsset video = CreateUploadedVideoAsset();
        video.MarkPendingProcessing(DateTime.UtcNow).IsSuccess.Should().BeTrue();

        var result = video.CompleteProcessing(DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        video.Status.Should().Be(MediaStatus.PENDING_PROCESSING);
        video.FinalKey.Should().Be(StorageKey.None);
    }

    [Fact]
    public void FailProcessing_ShouldMoveProcessingVideoToFailedState()
    {
        VideoAsset video = CreateProcessingVideoAsset();

        var result = video.FailProcessing(DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        video.Status.Should().Be(MediaStatus.FAILED);
    }

    [Fact]
    public void FailedVideo_ShouldNotMoveBackToProcessingOrReady()
    {
        VideoAsset video = CreateProcessingVideoAsset();
        video.FailProcessing(DateTime.UtcNow).IsSuccess.Should().BeTrue();

        var startAgainResult = video.StartProcessing(DateTime.UtcNow);
        var completeResult = video.CompleteProcessing(DateTime.UtcNow);

        startAgainResult.IsFailure.Should().BeTrue();
        completeResult.IsFailure.Should().BeTrue();
        video.Status.Should().Be(MediaStatus.FAILED);
        video.FinalKey.Should().Be(StorageKey.None);
    }

    [Fact]
    public void StartProcessing_ShouldBeIdempotent_WhenVideoIsAlreadyProcessing()
    {
        VideoAsset video = CreateProcessingVideoAsset();

        var result = video.StartProcessing(DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        video.Status.Should().Be(MediaStatus.PROCESSING);
    }

    [Fact]
    public void SetMetadata_ShouldStoreVideoMetadata()
    {
        VideoAsset video = CreateVideoAsset();
        var metadata = VideoMetadata.Create(
            TimeSpan.FromSeconds(120),
            1920,
            1080,
            "h264",
            "mp4").Value;

        var result = video.SetMetadata(metadata);

        result.IsSuccess.Should().BeTrue();
        video.Metadata.Should().Be(metadata);
    }

    private static VideoAsset CreateUploadedVideoAsset()
    {
        VideoAsset video = CreateVideoAsset();
        video.MarkUploaded(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        return video;
    }

    private static VideoAsset CreateProcessingVideoAsset()
    {
        VideoAsset video = CreateUploadedVideoAsset();
        video.MarkPendingProcessing(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        video.StartProcessing(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        return video;
    }

    private static VideoAsset CreateVideoAsset()
    {
        MediaData mediaData = CreateVideoMediaData();
        MediaOwner owner = MediaOwner.ForLesson(Guid.NewGuid()).Value;

        return VideoAsset.CreateForUpload(Guid.NewGuid(), mediaData, owner).Value;
    }

    private static MediaData CreateVideoMediaData()
    {
        FileName fileName = FileName.Create("lecture.mp4").Value;
        ContentType contentType = ContentType.Create("video/mp4").Value;

        return MediaData.Create(
            fileName,
            contentType,
            size: 1024,
            expectedChunksCount: 1).Value;
    }
}