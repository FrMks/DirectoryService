using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Core.Files;
using FileService.Domain.Entities.MediaAssetEntity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Framework.EndpointResults;

namespace FileService.Core.Multipart;

public static class AbortMultipartUpload
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/files/abort-multipart-upload", async Task<Microsoft.AspNetCore.Http.IResult> (
            [FromBody] AbortMultipartUploadDto abortDto,
            [FromServices] AbortMultipartUploadHandler handler,
            CancellationToken cancellationToken) =>
        {
            CSharpFunctionalExtensions.UnitResult<Error> result = await handler.Handle(abortDto, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(Envelope.Ok())
                : new ErrorsResult(result.Error);
        });

        return endpoints;
    }
}

public sealed class AbortMultipartUploadHandler
{
    private readonly ILogger<AbortMultipartUploadHandler> _logger;
    private readonly IS3Provider _s3Provider;
    private readonly IMediaRepository _mediaRepository;

    public AbortMultipartUploadHandler(
        ILogger<AbortMultipartUploadHandler> logger,
        IS3Provider s3Provider,
        IMediaRepository mediaRepository)
    {
        _logger = logger;
        _s3Provider = s3Provider;
        _mediaRepository = mediaRepository;
    }

    public async Task<UnitResult<Error>> Handle(
        AbortMultipartUploadDto abortDto,
        CancellationToken cancellationToken)
    {
        Result<MediaAsset, Error> mediaAssetResult = await _mediaRepository
            .GetBy(
                x => x.Id == abortDto.MediaAssetId,
                cancellationToken);
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error;

        MediaAsset mediaAsset = mediaAssetResult.Value;

        if (mediaAsset.Status == Domain.Enums.MediaStatus.DELETED)
            return UnitResult.Success<Error>();

        if (mediaAsset.Status != Domain.Enums.MediaStatus.UPLOADING)
        {
            return Error.Validation(
                "media.asset.status",
                $"Can not abot media asset when status is {mediaAsset.Status}");
        }

        UnitResult<Error> result = await _s3Provider
            .AbortMultipartUploadAsync(mediaAsset.RawKey, abortDto.UploadId, cancellationToken);
        if (result.IsFailure)
            return result.Error;

        UnitResult<Error> markDeletedResult = mediaAsset.MarkDeleted(DateTime.UtcNow);
        if (markDeletedResult.IsFailure)
            return markDeletedResult.Error;

        await _mediaRepository.UpdateAsync(mediaAsset, cancellationToken);

        return UnitResult.Success<Error>();
    }
}
