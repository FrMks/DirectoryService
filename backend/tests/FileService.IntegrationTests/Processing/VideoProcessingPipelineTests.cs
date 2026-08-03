using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Core.Multipart;
using FileService.Domain.Entities;
using FileService.Domain.Entities.MediaAssetEntity;
using FileService.Domain.Enums;
using FileService.Domain.MediaProcessing;
using FileService.Domain.ValueObjects;
using FileService.Infrastructure.Postgres;
using FileService.IntegrationTests.Infrastructure;
using FileService.VideoProcessing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared;

namespace FileService.IntegrationTests.Processing;

public class VideoProcessingPipelineTests : FileServiceBaseTests
{
    private const string VideoContentType = "video/mp4";

    public VideoProcessingPipelineTests(FileServiceTestWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task ProcessVideoAsync_WhenInvalidVideoUploaded_ShouldCompleteProcessingWithFailure()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        IVideoProcessingService videoProcessingService = scope.ServiceProvider
            .GetRequiredService<IVideoProcessingService>();

        Guid videoAssetId = await UploadTestVideoAsync(GetInvalidVideoPath(), cancellationToken);

        UnitResult<Error> result = await videoProcessingService.ProcessVideoAsync(
            videoAssetId,
            cancellationToken);

        result.IsFailure.Should().BeTrue();

        VideoAsset videoAsset = await ExecuteInDb(db => db.VideoAssets
            .FirstAsync(x => x.Id == videoAssetId, cancellationToken));
        videoAsset.Status.Should().Be(MediaStatus.FAILED);

        VideoProcess videoProcess = await ExecuteInDb(db => db.VideoProcess
            .Include(x => x.Steps)
            .FirstAsync(x => x.VideoAssetId == videoAssetId, cancellationToken));
        videoProcess.Status.Should().Be(ProcessingStatus.FAILED);
        videoProcess.Steps
            .Single(x => x.StepType == StepType.EXTRACT_METADATA)
            .Status.Should().Be(StepStatus.FAILED);
    }

    [Fact]
    public async Task ProcessVideoAsync_WhenValidVideoUploaded_ShouldCompleteProcessingSuccessfully()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        IVideoProcessingService videoProcessingService = scope.ServiceProvider
            .GetRequiredService<IVideoProcessingService>();

        Guid videoAssetId = await UploadTestVideoAsync(GetTestVideoPath(), cancellationToken);

        UnitResult<Error> result = await videoProcessingService.ProcessVideoAsync(
            videoAssetId,
            cancellationToken);

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Message : null);

        VideoAsset videoAsset = await ExecuteInDb(db => db.VideoAssets
            .FirstAsync(x => x.Id == videoAssetId, cancellationToken));
        videoAsset.Status.Should().Be(MediaStatus.READY);
        videoAsset.Metadata.Should().NotBeNull();
        videoAsset.PreviewKey.Should().NotBeNull();
        videoAsset.FinalKey.Value.Should().Be($"hls/{videoAssetId}/{VideoAsset.MASTER_PLAYLIST_NAME}");
        videoAsset.HlsResult.ManifestKey.Value.Should().Be(videoAsset.FinalKey.Value);

        VideoProcess videoProcess = await ExecuteInDb(db => db.VideoProcess
            .Include(x => x.Steps)
            .FirstAsync(x => x.VideoAssetId == videoAssetId, cancellationToken));
        videoProcess.Status.Should().Be(ProcessingStatus.COMPLETED);
        videoProcess.ProgressPercentage.Should().Be(100);
        videoProcess.CompletedAt.Should().NotBeNull();
        videoProcess.ErrorMessage.Should().BeNull();
        videoProcess.Steps.Should().OnlyContain(x => x.Status == StepStatus.COMPLETED);

        await ExecuteWithStorage(async storage =>
        {
            Result<StorageObjectMetadata, Error> hlsMetadataResult = await storage.GetMetadataAsync(
                videoAsset.FinalKey,
                cancellationToken);
            hlsMetadataResult.IsSuccess.Should().BeTrue();

            Result<StorageObjectMetadata, Error> previewMetadataResult = await storage.GetMetadataAsync(
                videoAsset.PreviewKey!,
                cancellationToken);
            previewMetadataResult.IsSuccess.Should().BeTrue();
            previewMetadataResult.Value.ContentType.Should().Be("image/jpeg");

            Result<StorageObjectMetadata, Error> rawMetadataResult = await storage.GetMetadataAsync(
                videoAsset.RawKey,
                cancellationToken);
            rawMetadataResult.IsFailure.Should().BeTrue();
        });
    }

    private async Task<Guid> UploadTestVideoAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        FileInfo videoFile = new(filePath);

        Result<FileName, Error> fileNameResult = FileName.Create(videoFile.Name);
        fileNameResult.IsSuccess.Should().BeTrue();

        Result<ContentType, Error> contentTypeResult = ContentType.Create(VideoContentType);
        contentTypeResult.IsSuccess.Should().BeTrue();

        Result<MediaData, Error> mediaDataResult = MediaData.Create(
            fileNameResult.Value,
            contentTypeResult.Value,
            videoFile.Length,
            expectedChunksCount: 1);
        mediaDataResult.IsSuccess.Should().BeTrue();

        Result<MediaOwner, Error> ownerResult = MediaOwner.ForLesson(Guid.NewGuid());
        ownerResult.IsSuccess.Should().BeTrue();

        Result<VideoAsset, Error> videoAssetResult = VideoAsset.CreateForUpload(
            Guid.NewGuid(),
            mediaDataResult.Value,
            ownerResult.Value);
        videoAssetResult.IsSuccess.Should().BeTrue();

        VideoAsset videoAsset = videoAssetResult.Value;

        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        IS3Provider storage = scope.ServiceProvider.GetRequiredService<IS3Provider>();
        FileServiceDbContext dbContext = scope.ServiceProvider.GetRequiredService<FileServiceDbContext>();

        await using FileStream videoStream = videoFile.OpenRead();
        UnitResult<Error> uploadResult = await storage.UploadFileAsync(
            videoAsset.RawKey,
            videoStream,
            VideoContentType,
            cancellationToken);
        uploadResult.IsSuccess.Should().BeTrue(uploadResult.IsFailure ? uploadResult.Error.Message : null);

        Result<StorageObjectMetadata, Error> metadataResult = await storage.GetMetadataAsync(
            videoAsset.RawKey,
            cancellationToken);
        metadataResult.IsSuccess.Should().BeTrue(metadataResult.IsFailure ? metadataResult.Error.Message : null);

        Result<StorageReference, Error> storageReferenceResult = StorageReference.Create(
            videoAsset.RawKey,
            metadataResult.Value.SizeBytes,
            metadataResult.Value.ContentType,
            metadataResult.Value.ETag);
        storageReferenceResult.IsSuccess.Should().BeTrue();

        UnitResult<Error> attachResult = videoAsset.AttachUploadedObject(storageReferenceResult.Value);
        attachResult.IsSuccess.Should().BeTrue(attachResult.IsFailure ? attachResult.Error.Message : null);

        UnitResult<Error> markUploadedResult = videoAsset.MarkUploaded(DateTime.UtcNow);
        markUploadedResult.IsSuccess.Should().BeTrue(markUploadedResult.IsFailure ? markUploadedResult.Error.Message : null);

        dbContext.MediaAssets.Add(videoAsset);
        await dbContext.SaveChangesAsync(cancellationToken);

        return videoAsset.Id;
    }

    private static string GetTestVideoPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Resources", "summary.mp4");
    }

    private static string GetInvalidVideoPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Resources", "txtFirst.mp4");
    }
}
