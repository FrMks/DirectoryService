using FileService.Core.Files.FileKey;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using FileService.Domain.ValueObjects;
using Shared;
using Shared.Framework.EndpointResults;

namespace FileService.Core.Files;

public static class UploadEndpoint
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/files", async Task<IResult> (
            [FromForm] IFormFile formFile,
            [FromServices] IS3Provider storage,
            [FromServices] IFileKeyGenerator fileKeyGenerator,
            CancellationToken cancellationToken) =>
        {
            var key = fileKeyGenerator.GenerateRawFileKey(
                new FileKeyContext(formFile.FileName, formFile.ContentType));
            await using Stream stream = formFile.OpenReadStream();

            var storageKeyResult = StorageKey.Create("preview", null, key);
            if (storageKeyResult.IsFailure)
            {
                return new ErrorsResult(storageKeyResult.Error);
            }

            var fileNameResult = FileName.Create(formFile.FileName);
            if (fileNameResult.IsFailure)
            {
                return new ErrorsResult(fileNameResult.Error);
            }

            var contentTypeResult = ContentType.Create(formFile.ContentType);
            if (contentTypeResult.IsFailure)
            {
                return new ErrorsResult(contentTypeResult.Error);
            }

            var mediaDataResult = MediaData.Create(
                fileNameResult.Value,
                contentTypeResult.Value,
                formFile.Length,
                1);
            if (mediaDataResult.IsFailure)
            {
                return new ErrorsResult(mediaDataResult.Error);
            }

            var uploadResult = await storage.UploadFileAsync(
                storageKeyResult.Value,
                stream,
                mediaDataResult.Value,
                cancellationToken);

            if (uploadResult.IsFailure)
            {
                return new ErrorsResult(uploadResult.Error);
            }

            return Results.Ok(Envelope.Ok(new { key = storageKeyResult.Value.Value }));
        }).DisableAntiforgery();

        return endpoints;
    }
}
