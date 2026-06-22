using System.Net.Http.Headers;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Domain.ValueObjects;
using FileService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Shared;

namespace FileService.IntegrationTests.Storage;

public class S3ProviderStorageFlowTests : FileServiceBaseTests
{
    public S3ProviderStorageFlowTests(FileServiceTestWebFactory factory)
        : base(factory)
    {
    }

    // upload Url => Put => metadata => download Url => Get => delete
    [Fact]
    public async Task UploadUrl_UploadsFileAndMetadataMatches()
    {
        // Arrange
        byte[] bytes = [1, 2, 3, 4, 5];

        StorageKey key = StorageKey.Create(
            "preview",
            "raw",
            Guid.NewGuid().ToString()).Value;

        MediaData mediaData = MediaData.Create(
            FileName.Create("preview.png").Value,
            ContentType.Create("image/png").Value,
            bytes.Length,
            expectedChunksCount: 1).Value;

        // Act
        string uploadUrl = await ExecuteWithStorage(async storage =>
        {
            Result<string, Error> result = await storage.GenerateUploadUrlAsync(
                key,
                mediaData,
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            return result.Value;
        });

        // Добавляем данные в uploadUrl
        // HttpClient используется когда .NET-приложение само выступает HTTP-клиентом
        // и отправляет запросы во внешний HTTP-сервис
        using var httpClient = new HttpClient();

        // Для отправки media используется ByteArrayContent, для например строки будет исопльзоваться другое
        using var content = new ByteArrayContent(bytes);
        // MediaTypeHeaderValue - объект для записи внутрь headers внутрь запроса
        // method: PUT utl: ... headers: Content-Type, Authorization... body: bytes
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        HttpResponseMessage uploadResponse = await httpClient
            .PutAsync(uploadUrl, content);

        uploadResponse.IsSuccessStatusCode.Should().BeTrue();

        // Получаем metadata из хранилища и сравниваем с тем, что мы загрузили
        StorageObjectMetadata metadata = await ExecuteWithStorage(async storage =>
        {
            Result<StorageObjectMetadata, Error> result = await storage.GetMetadataAsync(
                key,
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            return result.Value;
        });

        metadata.ContentType.Should().Be("image/png");
        metadata.SizeBytes.Should().Be(bytes.Length);
        metadata.ETag.Should().NotBeNullOrWhiteSpace();

        // Download
        string downloadUrl = await ExecuteWithStorage(async storage =>
        {
            Result<string, Error> result = await storage.GenerateDownloadUrlAsync(key);

            result.IsSuccess.Should().BeTrue();
            return result.Value;
        });

        byte[] downloadedBytes = await httpClient.GetByteArrayAsync(downloadUrl);

        downloadedBytes.Should().Equal(bytes);

        // Delete
        await ExecuteWithStorage(async storage =>
        {
            Result<string, Error> result = await storage.DeleteFileAsync(
                key,
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        });

        // Еще раз проверка metadata, что больше не читается
        await ExecuteWithStorage(async storage =>
        {
            Result<StorageObjectMetadata, Error> result = await storage.GetMetadataAsync(
                key,
                CancellationToken.None);

            result.IsFailure.Should().BeTrue();
        });
    }
}