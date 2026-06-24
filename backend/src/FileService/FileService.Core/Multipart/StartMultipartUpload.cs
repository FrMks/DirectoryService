using CSharpFunctionalExtensions;
using FileService.Contracts;
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

namespace FileService.Core.Files;

public static class StartMultipartUpload
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/files/multipart-upload", async Task<EndpointResult<StartMultipartUploadResponse>> (
            [FromBody] StartMultipartUploadRequest request,
            [FromServices] StartMultipartUploadHandler handler,
            CancellationToken cancellationToken) =>
        {
            Result<StartMultipartUploadResponse, Error> result = await handler.Handle(request, cancellationToken);

            return result;
        });

        return endpoints;
    }
}

public sealed class StartMultipartUploadHandler
{
    private readonly ILogger<StartMultipartUploadHandler> _logger;
    private readonly IS3Provider _s3Provider;
    private readonly IChunkSizeCalculator _chunkSizeCalculator;
    private readonly IMediaRepository _mediaRepository;

    public StartMultipartUploadHandler(
        ILogger<StartMultipartUploadHandler> logger,
        IS3Provider s3Provider,
        IChunkSizeCalculator chunkSizeCalculator,
        IMediaRepository mediaRepository)
    {
        _logger = logger;
        _s3Provider = s3Provider;
        _chunkSizeCalculator = chunkSizeCalculator;
        _mediaRepository = mediaRepository;
    }

    public async Task<Result<StartMultipartUploadResponse, Error>> Handle(StartMultipartUploadRequest request, CancellationToken cancellationToken)
    {
        var fileNameResult = FileName.Create(request.FileName);
        if (fileNameResult.IsFailure)
            return fileNameResult.Error;

        var contentTypeResult = ContentType.Create(request.ContentType);
        if (contentTypeResult.IsFailure)
            return contentTypeResult.Error;

        Result<(long ChunkSize, int TotalChunks), Error> chunkCalculationResult = _chunkSizeCalculator
            .CalculateChunkSize(request.Size);
        if (chunkCalculationResult.IsFailure)
            return chunkCalculationResult.Error;

        Result<MediaData, Error> mediaDataResult = MediaData.Create(
            fileNameResult.Value,
            contentTypeResult.Value,
            request.Size,
            chunkCalculationResult.Value.TotalChunks);
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

        Result<MediaOwner, Error> mediaOwnerResult = MediaOwner.Create(request.Context, request.ContextId);
        if (mediaOwnerResult.IsFailure)
            return mediaOwnerResult.Error;

        Result<MediaAsset, Error> mediaAssetResult = MediaAsset.CreateForUpload(
            mediaDataResult.Value,
            assetType,
            mediaOwnerResult.Value);
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error;

        Result<Guid, Error> addMediaAssetResult = await _mediaRepository.AddAsync(mediaAssetResult.Value, cancellationToken);
        if (addMediaAssetResult.IsFailure)
            return addMediaAssetResult.Error;

        _logger.LogInformation(
            "Created media asset {MediaAssetId} with status {Status} before starting multipart upload",
            mediaAssetResult.Value.Id,
            mediaAssetResult.Value.Status);

        Result<string, Error> startUploadResult = await _s3Provider.StartMultipartUploadAsync(
            mediaAssetResult.Value.RawKey,
            mediaAssetResult.Value.MediaData,
            cancellationToken);
        if (startUploadResult.IsFailure)
            return startUploadResult.Error;

        Result<IReadOnlyList<ChunkUploadUrl>, Error> chunkUploadUrlsResult = await _s3Provider.GenerateAllChunkUploadUrlsAsync(
            mediaAssetResult.Value.RawKey,
            startUploadResult.Value,
            chunkCalculationResult.Value.TotalChunks,
            cancellationToken);
        if (chunkUploadUrlsResult.IsFailure)
            return chunkUploadUrlsResult.Error;

        _logger.LogInformation(
            "Started multipart upload {UploadId} for media asset {MediaAssetId}",
            startUploadResult.Value,
            mediaAssetResult.Value.Id);

        return new StartMultipartUploadResponse(
            mediaAssetResult.Value.Id,
            startUploadResult.Value,
            chunkUploadUrlsResult.Value,
            chunkCalculationResult.Value.ChunkSize);
    }
}
