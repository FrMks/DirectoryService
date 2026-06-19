using System.Net.Http.Json;
using System.Net;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Domain.Entities.MediaAssetEntity;
using FileService.Domain.Enums;
using FileService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace FileService.IntegrationTests.Storage;

public class MultipartUploadTests : FileServiceBaseTests
{
    private const string ContentType = "video/mp4";
    private const string AssetType = "VIDEO";

    public MultipartUploadTests(FileServiceTestWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task MultipartUpload_Complete_WithAllParts_Succeeds()
    {
        // Arrange
        byte[] bytes = new byte[27 * 1024 * 1024];
        Random.Shared.NextBytes(bytes);

        // Act
        StartMultipartUploadResponse upload = await StartMultipartUpload(bytes.Length);

        List<PartETagDto> partETags = await UploadPartsAsync(upload, bytes);

        Contracts.CompleteMultipartUploadRequest completeMultipartUploadRequest = new(
            upload.MediaAssetId,
            upload.UploadId,
            partETags);

        HttpResponseMessage completeResponse = await Client.PostAsJsonAsync(
            "/files/complete-upload",
            completeMultipartUploadRequest);

        completeResponse.IsSuccessStatusCode.Should().BeTrue();

        MediaAsset asset = await ExecuteInDb(db =>
            db.MediaAssets.FirstAsync(x => x.Id == upload.MediaAssetId));
        asset.Status.Should().Be(MediaStatus.UPLOADED);

        await ExecuteWithStorage(async storage =>
        {
            Result<StorageObjectMetadata, Error> result = await storage.GetMetadataAsync(
                asset.RawKey,
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.SizeBytes.Should().Be(bytes.Length);
            result.Value.ContentType.Should().Be(ContentType);
        });
    }

    [Fact]
    public async Task MultipartUpload_Complete_WithMissingPart_ReturnsBadRequest()
    {
        // Arrange
        byte[] bytes = new byte[27 * 1024 * 1024];
        Random.Shared.NextBytes(bytes);

        StartMultipartUploadResponse upload = await StartMultipartUpload(bytes.Length);
        List<PartETagDto> partETags = await UploadPartsAsync(upload, bytes);
        IReadOnlyList<PartETagDto> invalidPartETags = partETags
            .Take(partETags.Count - 1)
            .ToList();

        var completeMultipartUploadRequest = new CompleteMultipartUploadRequest(
            upload.MediaAssetId,
            upload.UploadId,
            invalidPartETags);

        // Act
        HttpResponseMessage completeResponse = await Client.PostAsJsonAsync(
            "/files/complete-upload",
            completeMultipartUploadRequest);

        // Assert
        completeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        MediaAsset asset = await ExecuteInDb(db =>
            db.MediaAssets.FirstAsync(x => x.Id == upload.MediaAssetId));

        asset.Status.Should().Be(MediaStatus.UPLOADING);
    }

    [Fact]
    public async Task MultipartUpload_Abort_MarksAssetDeletedAndPreventsComplete()
    {
        // Arrange
        byte[] bytes = new byte[27 * 1024 * 1024];
        Random.Shared.NextBytes(bytes);

        StartMultipartUploadResponse upload = await StartMultipartUpload(bytes.Length);
        List<PartETagDto> partETags = await UploadPartsAsync(upload, bytes);

        var abortRequest = new AbortMultipartUploadDto(
            upload.MediaAssetId,
            upload.UploadId);

        // Act
        HttpResponseMessage abortResponse = await Client.PostAsJsonAsync(
            "/files/abort-multipart-upload",
            abortRequest);

        // Assert
        abortResponse.IsSuccessStatusCode.Should().BeTrue();

        MediaAsset asset = await ExecuteInDb(db =>
            db.MediaAssets.FirstAsync(x => x.Id == upload.MediaAssetId));

        asset.Status.Should().Be(MediaStatus.DELETED);

        var completeRequest = new CompleteMultipartUploadRequest(
            upload.MediaAssetId,
            upload.UploadId,
            partETags);

        HttpResponseMessage completeResponse = await Client.PostAsJsonAsync(
            "/files/complete-upload",
            completeRequest);

        completeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await ExecuteWithStorage(async storage =>
        {
            Result<StorageObjectMetadata, Error> result = await storage.GetMetadataAsync(
                asset.RawKey,
                CancellationToken.None);

            result.IsFailure.Should().BeTrue();
        });
    }

    private async Task<List<PartETagDto>> UploadPartsAsync(
        StartMultipartUploadResponse upload,
        byte[] bytes)
    {
        using var httpClient = new HttpClient();

        List<PartETagDto> partETags = [];

        foreach (ChunkUploadUrl chunkUploadUrl in upload.ChunkUploadUrls)
        {
            int partNumber = chunkUploadUrl.PartNumber;

            long offset = (partNumber - 1) * upload.ChunkSize;
            int count = (int)Math.Min(upload.ChunkSize, bytes.Length - offset);

            byte[] chunkBytes = bytes
                .Skip((int)offset)
                .Take(count)
                .ToArray();

            using var content = new ByteArrayContent(chunkBytes);
            HttpResponseMessage response = await httpClient.PutAsync(
                chunkUploadUrl.UploadUrl,
                content);
            response.IsSuccessStatusCode.Should().BeTrue();
            response.Headers.ETag.Should().NotBeNull();

            partETags.Add(new PartETagDto(
                partNumber,
                response.Headers.ETag!.Tag));
        }

        return partETags;
    }

    private async Task<StartMultipartUploadResponse> StartMultipartUpload(
        long size,
        string context = "lesson",
        Guid? contextId = null)
    {
        StartMultipartUploadRequest startMultipartUploadRequest = new(
            "video.mp4",
            AssetType,
            ContentType,
            size,
            context,
            contextId ?? Guid.NewGuid());

        HttpResponseMessage startResponse = await Client.PostAsJsonAsync("/files/multipart-upload", startMultipartUploadRequest);
        startResponse.IsSuccessStatusCode.Should().BeTrue();

        StartMultipartUploadResponse? upload = await startResponse.Content.ReadFromJsonAsync<StartMultipartUploadResponse>();
        upload.Should().NotBeNull();
        upload!.MediaAssetId.Should().NotBeEmpty();
        upload.UploadId.Should().NotBeEmpty();
        upload.ChunkSize.Should().BeGreaterThan(1);
        upload.ChunkUploadUrls.Should().NotBeNullOrEmpty();

        return upload;
    }
}
