using System.Net.Http.Headers;
using System.Net.Http.Json;
using FileService.Contracts;
using FileService.Domain.Entities.MediaAssetEntity;
using FileService.Domain.Enums;
using FileService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FileService.IntegrationTests.Storage;

public class SimpleUploadTests : FileServiceBaseTests
{
    public SimpleUploadTests(FileServiceTestWebFactory factory)
        : base(factory)
    {
    }

    // Start upload => Get uploadurl and mediaAssetId
    // => create real PUT to MinIO by uploadUrl
    // => call complete endpoint
    // => check db that asset ready and uploaded object fill
    [Fact]
    public async Task Upload_Complete_MekesAssetReady()
    {
        byte[] bytes = [1, 2, 3, 4, 5];

        var request = new StartUploadRequest(
            FileName: "preview.png",
            AssetType: "PREVIEW",
            ContentType: "image/png",
            Size: bytes.Length,
            Context: "lesson",
            ContextId: Guid.NewGuid());

        HttpResponseMessage startResponse = await Client.PostAsJsonAsync("/files/uploads", request);
        startResponse.IsSuccessStatusCode.Should().BeTrue();

        StartUploadResponse? upload = await startResponse.Content.ReadFromJsonAsync<StartUploadResponse>();
        upload.Should().NotBeNull();
        upload!.MediaAssetId.Should().NotBeEmpty();
        upload.UploadUrl.Should().NotBeNullOrWhiteSpace();
        upload.Method.Should().Be("PUT");
        upload.RequiredHeaders.Should().ContainKey("Content-Type");
        upload.RequiredHeaders["Content-Type"].Should().Be("image/png");

        using var uploadHttpClient = new HttpClient();
        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        HttpResponseMessage putResponse = await uploadHttpClient.PutAsync(upload.UploadUrl, content);
        putResponse.IsSuccessStatusCode.Should().BeTrue();

        HttpResponseMessage completeResponse = await Client.PostAsync(
            $"/files/{upload.MediaAssetId}/complete",
            null);

        MediaAsset asset = await ExecuteInDb(db =>
            db.MediaAssets.FirstAsync(x => x.Id == upload.MediaAssetId));

        asset.Status.Should().Be(MediaStatus.READY);
        asset.UploadedObject.Should().NotBeNull();
        asset.UploadedObject!.SizeBytes.Should().Be(bytes.Length);
        asset.UploadedObject.ContentType.Should().Be("image/png");
        asset.UploadedObject.ETag.Should().NotBeNullOrWhiteSpace();
    }
}