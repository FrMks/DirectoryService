using FileService.Domain.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FileService.Core.Files;

public static class GetDownloadUrlEndpoint
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/files/url", async Task<IResult> (
            string bucket,
            string key,
            [FromServices] IS3Provider storage) =>
        {
            var storageKeyResult = StorageKey.Create(bucket, null, key);
            if (storageKeyResult.IsFailure)
            {
                return Results.BadRequest(storageKeyResult.Error);
            }

            var result = await storage.GenerateDownloadUrlAsync(storageKeyResult.Value);
            if (result.IsFailure)
            {
                return Results.BadRequest(result.Error);
            }

            return Results.Ok(result.Value);
        }).DisableAntiforgery();

        return endpoints;
    }
}
