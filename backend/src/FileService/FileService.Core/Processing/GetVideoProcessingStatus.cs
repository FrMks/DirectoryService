using CSharpFunctionalExtensions;
using FileService.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared;
using Shared.Framework.EndpointResults;

namespace FileService.Core.Processing;

public static class GetVideoProcessingStatus
{
    public static IEndpointRouteBuilder MapVideoProcessingStatusEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/files/{videoAssetId:guid}/processing-status", async Task<EndpointResult<VideoProcessingStatusResponse>> (
            [FromRoute] Guid videoAssetId,
            [FromServices] GetVideoProcessingStatusHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result<VideoProcessingStatusResponse, Error> result = await handler.Handle(videoAssetId, cancellationToken);

            return result;
        });

        return endpoints;
    }
}
