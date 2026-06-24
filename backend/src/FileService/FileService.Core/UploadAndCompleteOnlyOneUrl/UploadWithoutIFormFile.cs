using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Core.Files;
using FileService.Domain;
using FileService.Domain.Entities.MediaAssetEntity;
using FileService.Domain.Enums.AssetTypeEnum;
using FileService.Domain.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Framework.EndpointResults;

namespace FileService.Core.UploadAndCompleteOnlyOneUrl;

public static class UploadWithoutIFormFile
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/files/uploads", async Task<EndpointResult<StartUploadResponse>> (
            [FromBody] StartUploadRequest request,
            [FromServices] StartUploadHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result<StartUploadResponse, Error> result = await handler.Handle(request, cancellationToken);

            return result;
        });

        return endpoints;
    }
}

public sealed class StartUploadHandler
{
    private readonly ILogger<StartUploadHandler> _logger;
    private readonly IS3Provider _s3Provider;
    private readonly IMediaRepository _mediaRepository;

    public StartUploadHandler(
        ILogger<StartUploadHandler> logger,
        IS3Provider s3Provider,
        IMediaRepository mediaRepository)
    {
        _logger = logger;
        _s3Provider = s3Provider;
        _mediaRepository = mediaRepository;
    }

    public async Task<Result<StartUploadResponse, Error>> Handle(
        StartUploadRequest request,
        CancellationToken cancellationToken)
    {
        Result<FileName, Error> fileNameResult = FileName.Create(request.FileName);
        if (fileNameResult.IsFailure)
            return fileNameResult.Error;

        Result<ContentType, Error> contentTypeResult = ContentType.Create(request.ContentType);
        if (contentTypeResult.IsFailure)
            return contentTypeResult.Error;

        Result<MediaData, Error> mediaDataResult = MediaData.Create(
            fileNameResult.Value,
            contentTypeResult.Value,
            request.Size,
            1);
        if (mediaDataResult.IsFailure)
            return mediaDataResult.Error;

        AssetType assetType;
        try
        {
            assetType = request.AssetType.ToAssetType();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid asset type {AssetType}", request.AssetType);
            return Error.Validation("asset.type.invalid", ex.Message);
        }

        Result<MediaOwner, Error> mediaOwnerResult = MediaOwner.Create(
            request.Context,
            request.ContextId);
        if (mediaOwnerResult.IsFailure)
            return mediaOwnerResult.Error;

        Result<MediaAsset, Error> mediaAssetResult = MediaAsset.CreateForUpload(
            mediaDataResult.Value,
            assetType,
            mediaOwnerResult.Value);
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error;

        Result<Guid, Error> addMediaAssetResult = await _mediaRepository.AddAsync(
            mediaAssetResult.Value,
            cancellationToken);
        if (addMediaAssetResult.IsFailure)
            return addMediaAssetResult.Error;

        Result<string, Error> uploadUrlResult = await _s3Provider.GenerateUploadUrlAsync(
            mediaAssetResult.Value.RawKey,
            mediaAssetResult.Value.MediaData,
            cancellationToken);
        if (uploadUrlResult.IsFailure)
            return uploadUrlResult.Error;

        return new StartUploadResponse(
            mediaAssetResult.Value.Id,
            uploadUrlResult.Value,
            "PUT",
            DateTimeOffset.UtcNow.AddMinutes(15),
            new Dictionary<string, string>
            {
                ["Content-Type"] = mediaAssetResult.Value.MediaData.ContentType.Value,
            });
    }
}
