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
            var result = await storage.GenerateDownloadUrlAsync(bucket, key);

            return Results.Ok(result);
        }).DisableAntiforgery();

        return endpoints;
    }
}
