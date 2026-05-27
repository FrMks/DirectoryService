using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared;

namespace FileService.Core.Files;

public static class StartMultipartUpload
{
    // Endpoint like: http://localhost:5000/files/multipart-upload?fileName=example.jpg&assetType=pictures&contentType=image/jpeg
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/files/multipart-upload", async Task<EndpointResult> (
            [FromBody] StartMultipartUploadRequest request,
            [FromServices] StartMultipartUploadHandler handler,
            CancellationToken cancellationToken) => await handler.Handle(request, cancellationToken)
}

    public class StartMultipartUploadHandler
    {
        private readonly ILogger<StartMultipartUploadHandler> _logger;
        private readonly IS3Provider _s3Provider;
        private readonly IChunkSizeCalculator _chunkSizeCalculator;

        public StartMultipartUploadHandler(
            ILogger<StartMultipartUploadHandler> logger,
            IS3Provider s3Provider,
            IChunkSizeCalculator chunkSizeCalculator)
        {
            _logger = logger;
            _s3Provider = s3Provider;
            _chunkSizeCalculator = chunkSizeCalculator;
        }

        public async Task<Result<Guid, Error>> Handle(StartMultipartUploadRequest request, CancellationToken cancellationToken)
        {
            // Выполнить валидацию
            var fileNameResult = FileName.Create(request.FileName);
            if (fileNameResult.IsFailure)
                return fileNameResult.Error;

            var contentTypeResult = ContentType.Create(request.ContentType);
            if (contentTypeResult.IsFailure)
                return contentTypeResult.Error;

            // Посчитать количество чанков для загрузки файла и их размер



            var mediaData = MediaData.Create(
                fileNameResult.Value,
                contentTypeResult.Value,
                request.Size,


                );
            var mediaAssetResult = MediaAsset.CreateForUpload(, request.AssetType.ToAssetType());

            // Создать доменную сущность MediaAsset, вызвав метод CreateForUpload

            // начать multipart загрузку

            // сгенерировать коллекцию uploadUrl для чанков

            // если успешно, то сохранить в БД MediaAsset со статусом UPLOADING

            // вернуть данные mediaAsset (id), uploadId, коллекцию ссылок для загрузки чанков, размер чанка


            var startUploadResult = await _s3Provider.StartMultipartUploadAsync(
                bucketName: "my-bucket",
                key: request.FileName,
                contentType: "application/octet-stream",
                cancellationToken);

            return Result.Success<Guid, Error>(Guid.NewGuid());
        }
    }
}
