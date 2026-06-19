using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using FileService.Contracts;
using FileService.Domain.Entities.MediaAssetEntity;
using FileService.Domain.Enums;
using FileService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FileService.IntegrationTests.Storage;

public class SimpleUploadTests : FileServiceBaseTests
{
    private const string ContentType = "image/png";
    private const string AssetType = "PREVIEW";

    public SimpleUploadTests(FileServiceTestWebFactory factory)
        : base(factory)
    {
    }

    // Start upload => Get uploadurl and mediaAssetId
    // => create real PUT to MinIO by uploadUrl
    // => call complete endpoint
    // => check db that asset ready and uploaded object fill
    [Fact]
    public async Task Upload_Complete_MakesAssetReady()
    {
        byte[] bytes = [1, 2, 3, 4, 5];

        StartUploadResponse upload = await StartUploadAsync(bytes.Length);
        await PutFileToStorageAsync(upload.UploadUrl, bytes);

        HttpResponseMessage completeResponse = await Client.PostAsync(
            $"/files/{upload.MediaAssetId}/complete",
            content: null);
        completeResponse.IsSuccessStatusCode.Should().BeTrue();

        MediaAsset asset = await ExecuteInDb(db =>
            db.MediaAssets.FirstAsync(x => x.Id == upload.MediaAssetId));

        asset.Status.Should().Be(MediaStatus.READY);
        asset.UploadedObject.Should().NotBeNull();
        asset.UploadedObject!.SizeBytes.Should().Be(bytes.Length);
        asset.UploadedObject.ContentType.Should().Be(ContentType);
        asset.UploadedObject.ETag.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Complete_WithoutPut_ReturnsErrorAndKeepsAssetUploading()
    {
        byte[] bytes = [1, 2, 3, 4, 5];
        StartUploadResponse upload = await StartUploadAsync(bytes.Length);

        HttpResponseMessage completeResponse = await Client.PostAsync(
            $"/files/{upload.MediaAssetId}/complete",
            content: null);

        completeResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

        MediaAsset asset = await ExecuteInDb(db =>
            db.MediaAssets.FirstAsync(x => x.Id == upload.MediaAssetId));

        asset.Status.Should().Be(MediaStatus.UPLOADING);
        asset.UploadedObject.Should().BeNull();
    }

    [Fact]
    public async Task Complete_WhenAlreadyReady_ReturnsSuccess()
    {
        byte[] bytes = [1, 2, 3, 4, 5];
        StartUploadResponse upload = await StartUploadAsync(bytes.Length);
        await PutFileToStorageAsync(upload.UploadUrl, bytes);

        HttpResponseMessage firstCompleteResponse = await Client.PostAsync(
            $"/files/{upload.MediaAssetId}/complete",
            content: null);
        firstCompleteResponse.IsSuccessStatusCode.Should().BeTrue();

        HttpResponseMessage secondCompleteResponse = await Client.PostAsync(
            $"/files/{upload.MediaAssetId}/complete",
            content: null);

        secondCompleteResponse.IsSuccessStatusCode.Should().BeTrue();

        MediaAsset asset = await ExecuteInDb(db =>
            db.MediaAssets.FirstAsync(x => x.Id == upload.MediaAssetId));

        asset.Status.Should().Be(MediaStatus.READY);
        asset.UploadedObject.Should().NotBeNull();
    }

    [Fact]
    public async Task GetContentUrl_WhenAssetReady_ReturnsDownloadUrl()
    {
        byte[] bytes = [1, 2, 3, 4, 5];
        StartUploadResponse upload = await StartUploadAsync(bytes.Length);
        await PutFileToStorageAsync(upload.UploadUrl, bytes);

        HttpResponseMessage completeResponse = await Client.PostAsync(
            $"/files/{upload.MediaAssetId}/complete",
            content: null);
        completeResponse.IsSuccessStatusCode.Should().BeTrue();

        HttpResponseMessage contentUrlResponse = await Client.GetAsync(
            $"/files/{upload.MediaAssetId}/content-url");

        contentUrlResponse.IsSuccessStatusCode.Should().BeTrue();

        GetContentUrlResponse? contentUrl =
            await contentUrlResponse.Content.ReadFromJsonAsync<GetContentUrlResponse>();
        contentUrl.Should().NotBeNull();
        contentUrl!.MediaAssetId.Should().Be(upload.MediaAssetId);
        contentUrl.Url.Should().NotBeNullOrWhiteSpace();
        contentUrl.Method.Should().Be("GET");
        contentUrl.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);

        using var downloadHttpClient = new HttpClient();
        byte[] downloadedBytes = await downloadHttpClient.GetByteArrayAsync(contentUrl.Url);

        downloadedBytes.Should().Equal(bytes);
    }

    [Fact]
    public async Task GetContentUrl_WhenAssetIsNotReady_ReturnsBadRequest()
    {
        byte[] bytes = [1, 2, 3, 4, 5];
        StartUploadResponse upload = await StartUploadAsync(bytes.Length);

        HttpResponseMessage contentUrlResponse = await Client.GetAsync(
            $"/files/{upload.MediaAssetId}/content-url");

        contentUrlResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        MediaAsset asset = await ExecuteInDb(db =>
            db.MediaAssets.FirstAsync(x => x.Id == upload.MediaAssetId));

        asset.Status.Should().Be(MediaStatus.UPLOADING);
        asset.UploadedObject.Should().BeNull();
    }

    [Fact]
    public async Task GetFileById_WhenAssetReady_ReturnsMetadataAndContentUrl()
    {
        byte[] bytes = [1, 2, 3, 4, 5];
        StartUploadResponse upload = await StartUploadAsync(bytes.Length);
        await PutFileToStorageAsync(upload.UploadUrl, bytes);

        HttpResponseMessage completeResponse = await Client.PostAsync(
            $"/files/{upload.MediaAssetId}/complete",
            content: null);
        completeResponse.IsSuccessStatusCode.Should().BeTrue();

        HttpResponseMessage fileResponse = await Client.GetAsync($"/files/{upload.MediaAssetId}");

        fileResponse.IsSuccessStatusCode.Should().BeTrue();

        FileResponse? file = await fileResponse.Content.ReadFromJsonAsync<FileResponse>();
        file.Should().NotBeNull();
        file!.Id.Should().Be(upload.MediaAssetId);
        file.FileName.Should().Be("preview.png");
        file.ContentType.Should().Be(ContentType);
        file.Size.Should().Be(bytes.Length);
        file.Status.Should().Be(MediaStatus.READY.ToString());
        file.AssetType.Should().Be(AssetType);
        file.Context.Should().Be("lesson");
        file.ContextId.Should().NotBeEmpty();
        file.ContentUrl.Should().NotBeNullOrWhiteSpace();
        file.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
        file.UpdatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));

        using var downloadHttpClient = new HttpClient();
        byte[] downloadedBytes = await downloadHttpClient.GetByteArrayAsync(file.ContentUrl);

        downloadedBytes.Should().Equal(bytes);
    }

    [Fact]
    public async Task GetFileById_WhenAssetIsNotReady_ReturnsMetadataWithoutContentUrl()
    {
        byte[] bytes = [1, 2, 3, 4, 5];
        StartUploadResponse upload = await StartUploadAsync(bytes.Length);

        HttpResponseMessage fileResponse = await Client.GetAsync($"/files/{upload.MediaAssetId}");

        fileResponse.IsSuccessStatusCode.Should().BeTrue();

        FileResponse? file = await fileResponse.Content.ReadFromJsonAsync<FileResponse>();
        file.Should().NotBeNull();
        file!.Id.Should().Be(upload.MediaAssetId);
        file.FileName.Should().Be("preview.png");
        file.ContentType.Should().Be(ContentType);
        file.Size.Should().Be(bytes.Length);
        file.Status.Should().Be(MediaStatus.UPLOADING.ToString());
        file.AssetType.Should().Be(AssetType);
        file.Context.Should().Be("lesson");
        file.ContextId.Should().NotBeEmpty();
        file.ContentUrl.Should().BeNull();
    }

    [Fact]
    public async Task GetFilesByTargetEntity_ReturnsOnlyFilesForRequestedTargetEntity()
    {
        byte[] bytes = [1, 2, 3, 4, 5];
        Guid targetEntityId = Guid.NewGuid();
        Guid otherTargetEntityId = Guid.NewGuid();

        StartUploadResponse readyUpload = await StartUploadAsync(bytes.Length, contextId: targetEntityId);
        await PutFileToStorageAsync(readyUpload.UploadUrl, bytes);
        await CompleteUploadAsync(readyUpload.MediaAssetId);

        StartUploadResponse pendingUpload = await StartUploadAsync(bytes.Length, contextId: targetEntityId);

        StartUploadResponse otherUpload = await StartUploadAsync(bytes.Length, contextId: otherTargetEntityId);
        await PutFileToStorageAsync(otherUpload.UploadUrl, bytes);
        await CompleteUploadAsync(otherUpload.MediaAssetId);

        HttpResponseMessage response = await Client.GetAsync(
            $"/files?context=lesson&contextId={targetEntityId}");

        response.IsSuccessStatusCode.Should().BeTrue();

        IReadOnlyList<FileResponse>? files =
            await response.Content.ReadFromJsonAsync<IReadOnlyList<FileResponse>>();
        files.Should().NotBeNull();
        files!.Should().HaveCount(2);
        files.Select(x => x.Id).Should().BeEquivalentTo([
            readyUpload.MediaAssetId,
            pendingUpload.MediaAssetId,
        ]);

        FileResponse readyFile = files.Single(x => x.Id == readyUpload.MediaAssetId);
        readyFile.Status.Should().Be(MediaStatus.READY.ToString());
        readyFile.Context.Should().Be("lesson");
        readyFile.ContextId.Should().Be(targetEntityId);
        readyFile.ContentUrl.Should().NotBeNullOrWhiteSpace();

        FileResponse pendingFile = files.Single(x => x.Id == pendingUpload.MediaAssetId);
        pendingFile.Status.Should().Be(MediaStatus.UPLOADING.ToString());
        pendingFile.Context.Should().Be("lesson");
        pendingFile.ContextId.Should().Be(targetEntityId);
        pendingFile.ContentUrl.Should().BeNull();
    }

    [Fact]
    public async Task GetFilesByTargetEntity_DoesNotReturnDeletedFiles()
    {
        byte[] bytes = [1, 2, 3, 4, 5];
        Guid targetEntityId = Guid.NewGuid();

        StartUploadResponse upload = await StartUploadAsync(bytes.Length, contextId: targetEntityId);
        await PutFileToStorageAsync(upload.UploadUrl, bytes);
        await CompleteUploadAsync(upload.MediaAssetId);

        HttpResponseMessage deleteResponse = await Client.PostAsync(
            $"/files/{upload.MediaAssetId}",
            content: null);
        deleteResponse.IsSuccessStatusCode.Should().BeTrue();

        HttpResponseMessage response = await Client.GetAsync(
            $"/files?context=lesson&contextId={targetEntityId}");

        response.IsSuccessStatusCode.Should().BeTrue();

        IReadOnlyList<FileResponse>? files =
            await response.Content.ReadFromJsonAsync<IReadOnlyList<FileResponse>>();
        files.Should().NotBeNull();
        files.Should().BeEmpty();
    }

    private async Task<StartUploadResponse> StartUploadAsync(
        long size,
        string context = "lesson",
        Guid? contextId = null)
    {
        var request = new StartUploadRequest(
            FileName: "preview.png",
            AssetType: AssetType,
            ContentType: ContentType,
            Size: size,
            Context: context,
            ContextId: contextId ?? Guid.NewGuid());

        HttpResponseMessage startResponse = await Client.PostAsJsonAsync("/files/uploads", request);
        startResponse.IsSuccessStatusCode.Should().BeTrue();

        StartUploadResponse? upload = await startResponse.Content.ReadFromJsonAsync<StartUploadResponse>();
        upload.Should().NotBeNull();
        upload!.MediaAssetId.Should().NotBeEmpty();
        upload.UploadUrl.Should().NotBeNullOrWhiteSpace();
        upload.Method.Should().Be("PUT");
        upload.RequiredHeaders.Should().ContainKey("Content-Type");
        upload.RequiredHeaders["Content-Type"].Should().Be(ContentType);

        return upload;
    }

    private async Task CompleteUploadAsync(Guid mediaAssetId)
    {
        HttpResponseMessage completeResponse = await Client.PostAsync(
            $"/files/{mediaAssetId}/complete",
            content: null);

        completeResponse.IsSuccessStatusCode.Should().BeTrue();
    }

    private static async Task PutFileToStorageAsync(string uploadUrl, byte[] bytes)
    {
        using var uploadHttpClient = new HttpClient();
        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(ContentType);

        HttpResponseMessage putResponse = await uploadHttpClient.PutAsync(uploadUrl, content);
        putResponse.IsSuccessStatusCode.Should().BeTrue();
    }
}
