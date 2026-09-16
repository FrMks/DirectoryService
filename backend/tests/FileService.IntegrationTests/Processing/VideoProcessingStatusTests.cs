using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Core.Processing;
using FileService.Domain.Entities;
using FileService.Domain.Enums;
using FileService.Domain.MediaProcessing;
using FileService.Domain.ValueObjects;
using FileService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared;

namespace FileService.IntegrationTests.Processing;

public class VideoProcessingStatusTests : FileServiceBaseTests
{
    public VideoProcessingStatusTests(FileServiceTestWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetStatus_WhenVideoAssetDoesNotExist_ReturnsNotFound()
    {
        HttpResponseMessage response = await Client.GetAsync(
            $"/files/{Guid.NewGuid()}/processing-status");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetStatus_WhenProcessHasNotStarted_ReturnsQueuedState()
    {
        VideoAsset videoAsset = CreateVideoAsset();
        await SaveAsync(videoAsset);

        HttpResponseMessage response = await Client.GetAsync(
            $"/files/{videoAsset.Id}/processing-status");

        response.IsSuccessStatusCode.Should().BeTrue();
        VideoProcessingStatusResponse status =
            await response.ReadEnvelopeResultAsync<VideoProcessingStatusResponse>();
        status.AssetId.Should().Be(videoAsset.Id);
        status.Status.Should().Be("queued");
        status.CurrentStep.Should().BeNull();
        status.Percent.Should().Be(0);
        status.ErrorCode.Should().BeNull();
    }

    [Fact]
    public async Task Handler_WhenStepIsRunning_ReturnsProcessingSnapshot()
    {
        VideoAsset videoAsset = CreateVideoAsset();
        MoveToProcessing(videoAsset);
        VideoProcess videoProcess = new(videoAsset.Id);
        Result<ProcessingStep?, Error> stepResult = videoProcess.ProcessNextStep();
        stepResult.IsSuccess.Should().BeTrue();
        await SaveAsync(videoAsset, videoProcess);

        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        GetVideoProcessingStatusHandler handler = scope.ServiceProvider
            .GetRequiredService<GetVideoProcessingStatusHandler>();

        Result<VideoProcessingStatusResponse, Error> result = await handler.Handle(
            videoAsset.Id,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("processing");
        result.Value.CurrentStep.Should().Be("initialize");
        result.Value.Percent.Should().Be(0);
    }

    [Fact]
    public async Task GetStatus_WhenProcessingCompleted_ReturnsReadyState()
    {
        VideoAsset videoAsset = CreateVideoAsset();
        MoveToProcessing(videoAsset);
        VideoProcess videoProcess = CreateCompletedProcess(videoAsset.Id);
        videoAsset.CompleteProcessing(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        await SaveAsync(videoAsset, videoProcess);

        HttpResponseMessage response = await Client.GetAsync(
            $"/files/{videoAsset.Id}/processing-status");

        response.IsSuccessStatusCode.Should().BeTrue();
        VideoProcessingStatusResponse status =
            await response.ReadEnvelopeResultAsync<VideoProcessingStatusResponse>();
        status.Status.Should().Be("ready");
        status.CurrentStep.Should().BeNull();
        status.Percent.Should().Be(100);
        status.ErrorCode.Should().BeNull();
    }

    [Fact]
    public async Task GetStatus_WhenFailedProcessCanRetry_ReturnsQueuedState()
    {
        VideoAsset videoAsset = CreateVideoAsset();
        MoveToProcessing(videoAsset);
        VideoProcess videoProcess = CreateFailedProcess(videoAsset.Id, isCritical: false);
        videoAsset.FailProcessing(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        await SaveAsync(videoAsset, videoProcess);

        HttpResponseMessage response = await Client.GetAsync(
            $"/files/{videoAsset.Id}/processing-status");

        response.IsSuccessStatusCode.Should().BeTrue();
        VideoProcessingStatusResponse status =
            await response.ReadEnvelopeResultAsync<VideoProcessingStatusResponse>();
        status.Status.Should().Be("queued");
        status.CurrentStep.Should().BeNull();
        status.Percent.Should().Be(0);
        status.ErrorCode.Should().BeNull();
    }

    [Fact]
    public async Task Stream_WhenAssetIsReady_SendsInitialStateAndCompletes()
    {
        VideoAsset videoAsset = CreateVideoAsset();
        MoveToProcessing(videoAsset);
        VideoProcess videoProcess = CreateCompletedProcess(videoAsset.Id);
        videoAsset.CompleteProcessing(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        await SaveAsync(videoAsset, videoProcess);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using HttpResponseMessage response = await Client.GetAsync(
            $"/files/{videoAsset.Id}/processing-status/stream",
            HttpCompletionOption.ResponseHeadersRead,
            timeout.Token);

        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");
        response.Headers.CacheControl?.NoCache.Should().BeTrue();
        response.Headers.GetValues("X-Accel-Buffering").Should().ContainSingle("no");

        string body = await response.Content.ReadAsStringAsync(timeout.Token);
        SseEvent progressEvent = ParseSingleEvent(body);
        progressEvent.Name.Should().Be("progress");
        VideoProcessingStatusResponse status = DeserializeStatus(progressEvent.Data);
        status.Status.Should().Be("ready");
        status.Percent.Should().Be(100);
    }

    [Fact]
    public async Task Stream_WhenProcessingFailedPermanently_SendsFinalFailedState()
    {
        VideoAsset videoAsset = CreateVideoAsset();
        MoveToProcessing(videoAsset);
        VideoProcess videoProcess = CreateFailedProcess(videoAsset.Id, isCritical: true);
        videoAsset.FailProcessing(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        await SaveAsync(videoAsset, videoProcess);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using HttpResponseMessage response = await Client.GetAsync(
            $"/files/{videoAsset.Id}/processing-status/stream",
            HttpCompletionOption.ResponseHeadersRead,
            timeout.Token);

        string body = await response.Content.ReadAsStringAsync(timeout.Token);
        SseEvent progressEvent = ParseSingleEvent(body);
        VideoProcessingStatusResponse status = DeserializeStatus(progressEvent.Data);
        progressEvent.Name.Should().Be("progress");
        status.Status.Should().Be("failed");
        status.ErrorCode.Should().Be("video.processing.failed");
    }

    [Fact]
    public async Task Stream_WhenStateChanges_SendsInitialAndUpdatedStates()
    {
        VideoAsset videoAsset = CreateVideoAsset();
        await SaveAsync(videoAsset);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using HttpRequestMessage request = new(
            HttpMethod.Get,
            $"/files/{videoAsset.Id}/processing-status/stream");
        using HttpResponseMessage response = await Client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            timeout.Token);
        await using Stream stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using StreamReader reader = new(stream);

        SseEvent initialEvent = await ReadEventAsync(reader, timeout.Token);
        DeserializeStatus(initialEvent.Data).Status.Should().Be("queued");

        await ExecuteInDb(async dbContext =>
        {
            VideoAsset storedAsset = await dbContext.VideoAssets
                .FirstAsync(x => x.Id == videoAsset.Id, timeout.Token);
            MoveToProcessing(storedAsset);

            VideoProcess process = new(videoAsset.Id);
            process.ProcessNextStep().IsSuccess.Should().BeTrue();
            dbContext.VideoProcess.Add(process);
            await dbContext.SaveChangesAsync(timeout.Token);
        });

        SseEvent updatedEvent = await ReadEventAsync(reader, timeout.Token);
        updatedEvent.Name.Should().Be("progress");
        VideoProcessingStatusResponse updatedStatus = DeserializeStatus(updatedEvent.Data);
        updatedStatus.Status.Should().Be("processing");
        updatedStatus.CurrentStep.Should().Be("initialize");

        timeout.Cancel();
    }

    private async Task SaveAsync(VideoAsset videoAsset, VideoProcess? videoProcess = null)
    {
        await ExecuteInDb(async dbContext =>
        {
            dbContext.VideoAssets.Add(videoAsset);
            if (videoProcess is not null)
                dbContext.VideoProcess.Add(videoProcess);

            await dbContext.SaveChangesAsync();
        });
    }

    private static VideoAsset CreateVideoAsset()
    {
        Result<FileName, Error> fileName = FileName.Create("status-test.mp4");
        Result<ContentType, Error> contentType = ContentType.Create("video/mp4");
        Result<MediaOwner, Error> owner = MediaOwner.ForLesson(Guid.NewGuid());
        fileName.IsSuccess.Should().BeTrue();
        contentType.IsSuccess.Should().BeTrue();
        owner.IsSuccess.Should().BeTrue();

        Result<MediaData, Error> mediaData = MediaData.Create(
            fileName.Value,
            contentType.Value,
            size: 1,
            expectedChunksCount: 1);
        mediaData.IsSuccess.Should().BeTrue();

        Result<VideoAsset, Error> asset = VideoAsset.CreateForUpload(
            Guid.NewGuid(),
            mediaData.Value,
            owner.Value);
        asset.IsSuccess.Should().BeTrue();
        return asset.Value;
    }

    private static void MoveToProcessing(VideoAsset videoAsset)
    {
        videoAsset.MarkUploaded(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        videoAsset.MarkPendingProcessing(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        videoAsset.StartProcessing(DateTime.UtcNow).IsSuccess.Should().BeTrue();
    }

    private static VideoProcess CreateCompletedProcess(Guid videoAssetId)
    {
        VideoProcess process = new(videoAssetId);

        while (process.Status == ProcessingStatus.IN_PROGRESS)
        {
            Result<ProcessingStep?, Error> step = process.ProcessNextStep();
            step.IsSuccess.Should().BeTrue();
            if (step.Value is null)
                break;

            process.CompleteCurrentStep().IsSuccess.Should().BeTrue();
        }

        process.Status.Should().Be(ProcessingStatus.COMPLETED);
        return process;
    }

    private static VideoProcess CreateFailedProcess(Guid videoAssetId, bool isCritical)
    {
        VideoProcess process = new(videoAssetId);
        process.ProcessNextStep().IsSuccess.Should().BeTrue();
        process.FailCurrentStep("Test processing failure").IsSuccess.Should().BeTrue();
        process.Fail("Test processing failure", isCritical).IsSuccess.Should().BeTrue();
        return process;
    }

    private static SseEvent ParseSingleEvent(string body)
    {
        string[] lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string name = lines.Single(x => x.StartsWith("event: ", StringComparison.Ordinal))[7..];
        string data = lines.Single(x => x.StartsWith("data: ", StringComparison.Ordinal))[6..];
        return new SseEvent(name.Trim(), data.Trim());
    }

    private static async Task<SseEvent> ReadEventAsync(
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        string? name = null;
        string? data = null;

        while (true)
        {
            string? line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
                throw new EndOfStreamException("SSE stream ended before a complete event was received.");

            if (line.StartsWith("event: ", StringComparison.Ordinal))
                name = line[7..];
            else if (line.StartsWith("data: ", StringComparison.Ordinal))
                data = line[6..];
            else if (line.Length == 0 && name is not null && data is not null)
                return new SseEvent(name, data);
        }
    }

    private static VideoProcessingStatusResponse DeserializeStatus(string json)
    {
        return JsonSerializer.Deserialize<VideoProcessingStatusResponse>(
                   json,
                   JsonSerializerOptions.Web)
               ?? throw new InvalidOperationException("SSE event does not contain a processing status.");
    }

    private sealed record SseEvent(string Name, string Data);
}
