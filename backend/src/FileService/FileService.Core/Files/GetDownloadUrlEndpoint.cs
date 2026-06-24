using FileService.Domain.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared;
using Shared.Framework.EndpointResults;

namespace FileService.Core.Files;

public static class GetDownloadUrlEndpoint
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // /files/url?bucket=preview&key=image.png
        endpoints.MapGet("/files/url", async Task<IResult> (
            string bucket,
            string key,
            [FromServices] IS3Provider storage) =>
        {
            var storageKeyResult = StorageKey.Create(bucket, null, key);
            if (storageKeyResult.IsFailure)
            {
                return new ErrorsResult(storageKeyResult.Error);
            }

            var result = await storage.GenerateDownloadUrlAsync(storageKeyResult.Value);
            if (result.IsFailure)
            {
                return new ErrorsResult(result.Error);
            }

            return Results.Ok(Envelope.Ok(result.Value));
        }).DisableAntiforgery();

        return endpoints;
    }
}
