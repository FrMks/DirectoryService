using CSharpFunctionalExtensions;
using FileService.Core.Files;
using FileService.Domain.Entities.MediaAssetEntity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared;

namespace FileService.Core.UploadAndCompleteOnlyOneUrl;

public static class CompleteUpload
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/files/{mediaAssetId:guid}/complete", async Task<Microsoft.AspNetCore.Http.IResult> (
            [FromRoute] Guid mediaAssetId,
            [FromServices] CompleteUploadHandler handler,
            CancellationToken cancellationToken) =>
        {
            UnitResult<Error> result = await handler.Handle(mediaAssetId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok()
                : Results.BadRequest(result.Error);
        });

        return endpoints;
    }
}

public sealed class CompleteUploadHandler
{
    private readonly ILogger<CompleteUploadHandler> _logger;
    private readonly IS3Provider _s3Provider;
    private readonly IMediaRepository _mediaRepository;

    public CompleteUploadHandler(
        ILogger<CompleteUploadHandler> logger,
        IS3Provider s3Provider,
        IMediaRepository mediaRepository)
    {
        _logger = logger;
        _s3Provider = s3Provider;
        _mediaRepository = mediaRepository;
    }

    public async Task<UnitResult<Error>> Handle(
        Guid mediaAssetIt,
        CancellationToken cancellationToken)
    {
        Result<MediaAsset, Error> mediaAssetResult = await _mediaRepository
            .GetBy(m => m.Id == mediaAssetIt, cancellationToken);
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error;

        if (mediaAssetResult.Value.Status == Domain.Enums.MediaStatus.DELETED
            || mediaAssetResult.Value.Status == Domain.Enums.MediaStatus.FAILED)
            return mediaAssetResult.Error;
    }
}