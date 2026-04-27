using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FileService.Core.Files;

public static class UploadEndpoint
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/files", async Task<IResult> (
            [FromForm] IFormFile formFile,
            [FromServices] IS3Provider storage,
            CancellationToken cancellationToken) =>
        {
            var key = $"raw/{Guid.NewGuid()}";
            await using var stream = formFile.OpenReadStream();

            await storage.UploadFileAsync(
                stream,
                "pictures",
                key,
                formFile.ContentType,
                cancellationToken);

            return Results.Ok(new { key });
        }).DisableAntiforgery();

        return endpoints;
    }
}
