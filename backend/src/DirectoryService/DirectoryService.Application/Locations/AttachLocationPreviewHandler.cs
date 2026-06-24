using CSharpFunctionalExtensions;
using DirectoryService.Application.Locations.Interfaces;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Locations.ValueObjects;
using FileService.Communication;
using FileService.Contracts;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Core.Abstractions;
using Shared.Core.Database;

namespace DirectoryService.Application.Locations;

public class AttachLocationPreviewHandler(
    ILocationsRepository locationsRepository,
    IFileCommunicationService fileCommunicationService,
    ITransactionManager transactionManager,
    ILogger<AttachLocationPreviewHandler> logger)
    : ICommandHandler<Guid, AttachLocationPreviewCommand>
{
    public async Task<Result<Guid, Errors>> Handle(
        AttachLocationPreviewCommand command,
        CancellationToken cancellationToken)
    {
        LocationId locationId = LocationId.FromValue(command.LocationId);
        Result<Location, Error> locationResult = await locationsRepository
            .GetBy(l => l.Id == locationId, cancellationToken);
        if (locationResult.IsFailure)
        {
            logger.LogError(
                "Cannot attach location preview," +
                "becouse have problem: {error} when try to get location by id", locationResult.Error.Message);
            return locationResult.Error.ToErrors();
        }
        Location location = locationResult.Value;

        Result<FileResponse, Errors> mediaAssetResult = await fileCommunicationService
            .GetMediaAssetByIdAsync(command.Request.MediaAssetid, cancellationToken);
        if (mediaAssetResult.IsFailure)
        {
            logger.LogError(
                "Cannot attach location preview," +
                "becouse have problem: {error} when try to get media asset by id", mediaAssetResult.Error);
            return mediaAssetResult.Error;
        }
        FileResponse mediaAsset = mediaAssetResult.Value;
        if (!string.Equals(mediaAsset.Status, "READY", StringComparison.OrdinalIgnoreCase))
            return Error.Validation("location.preview.asset.not.ready", "Location preview asset must be ready.").ToErrors();

        if (!string.Equals(mediaAsset.AssetType, "PREVIEW", StringComparison.OrdinalIgnoreCase))
            return Error.Validation("location.preview.asset.invalid.type", "Location preview asset must be a preview asset.").ToErrors();

        if (!mediaAsset.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return Error.Validation("location.preview.asset.invalid.content-type", "Location preview asset must be an image.").ToErrors();

        if (!string.Equals(mediaAsset.Context, "location", StringComparison.OrdinalIgnoreCase)
            || mediaAsset.ContextId != location.Id.Value)
        {
            return Error.Validation("location.preview.asset.invalid.owner", "Location preview asset belongs to another entity.").ToErrors();
        }

        MediaAssetId mediaAssetIdResult = MediaAssetId.FromValue(mediaAsset.Id);
        Result<LocationPreviewMetadata, Error> locationPreviewMetadataResult = LocationPreviewMetadata
            .Create(
                mediaAssetIdResult,
                mediaAsset.FileName,
                mediaAsset.ContentType,
                mediaAsset.Size,
                DateTime.UtcNow,
                DateTime.UtcNow);
        if (locationPreviewMetadataResult.IsFailure)
        {
            logger.LogError(
                "Cannot attach location preview," +
                "becouse have problem: {error} when try to create LocationPreviewMetadata", mediaAssetResult.Error);
            return locationPreviewMetadataResult.Error.ToErrors();
        }

        location.AttachPreview(locationPreviewMetadataResult.Value);

        UnitResult<Error> saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            logger.LogError(
                "Cannot attach location preview because save failed: {Error}",
                saveResult.Error.Message);

            return saveResult.Error.ToErrors();
        }

        return location.Id.Value;
    }
}