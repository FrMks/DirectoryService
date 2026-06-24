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

public class SetLocationPreviewHandler(
    ILocationsRepository locationsRepository,
    IFileCommunicationService fileCommunicationService,
    ITransactionManager transactionManager,
    ILogger<SetLocationPreviewHandler> logger)
    : ICommandHandler<Guid, SetLocationPreviewCommand>
{
    private const string LocationPreviewContext = "location";
    private const string PreviewAssetType = "PREVIEW";
    private const string ReadyStatus = "READY";
    private const string DeletedStatus = "DELETED";

    public async Task<Result<Guid, Errors>> Handle(
        SetLocationPreviewCommand command,
        CancellationToken cancellationToken)
    {
        Result<Location, Error> locationResult = await locationsRepository
            .GetBy(l => l.Id == command.LocationId, cancellationToken);
        if (locationResult.IsFailure)
        {
            logger.LogError(
                "Cannot set location preview because location {LocationId} was not found: {Error}",
                command.LocationId,
                locationResult.Error.Message);
            return locationResult.Error.ToErrors();
        }

        Location location = locationResult.Value;

        Result<FileResponse, Errors> mediaAssetResult = await fileCommunicationService
            .GetMediaAssetByIdAsync(command.Request.MediaAssetId, cancellationToken);
        if (mediaAssetResult.IsFailure)
        {
            logger.LogError(
                "Cannot set location preview because FileService rejected media asset {MediaAssetId}: {Error}",
                command.Request.MediaAssetId,
                mediaAssetResult.Error);
            return mediaAssetResult.Error;
        }

        FileResponse mediaAsset = mediaAssetResult.Value;

        if (mediaAsset.Id != command.Request.MediaAssetId)
        {
            return Error.Validation(
                "location.preview.asset.id.mismatch",
                "File Service returned another media asset.").ToErrors();
        }

        if (string.Equals(mediaAsset.Status, DeletedStatus, StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation(
                "location.preview.asset.deleted",
                "Location preview asset must not be deleted.").ToErrors();
        }

        if (!string.Equals(mediaAsset.Status, ReadyStatus, StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation("location.preview.asset.not.ready", "Location preview asset must be ready.").ToErrors();
        }

        if (!string.Equals(mediaAsset.AssetType, PreviewAssetType, StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation("location.preview.asset.invalid.type", "Location preview asset must be a preview asset.").ToErrors();
        }

        if (!mediaAsset.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation("location.preview.asset.invalid.content-type", "Location preview asset must be an image.").ToErrors();
        }

        if (!string.Equals(mediaAsset.Context, LocationPreviewContext, StringComparison.OrdinalIgnoreCase)
            || mediaAsset.ContextId != location.Id.Value)
        {
            return Error.Validation("location.preview.asset.invalid.owner", "Location preview asset belongs to another entity.").ToErrors();
        }

        MediaAssetId mediaAssetIdResult = MediaAssetId.FromValue(mediaAsset.Id);
        DateTime verifiedAt = DateTime.UtcNow;

        Result<LocationPreviewMetadata, Error> locationPreviewMetadataResult = LocationPreviewMetadata
            .Create(
                mediaAssetIdResult,
                mediaAsset.FileName,
                mediaAsset.ContentType,
                mediaAsset.Size,
                verifiedAt,
                verifiedAt);
        if (locationPreviewMetadataResult.IsFailure)
        {
            logger.LogError(
                "Cannot set location preview because metadata creation failed: {Error}",
                locationPreviewMetadataResult.Error.Message);
            return locationPreviewMetadataResult.Error.ToErrors();
        }

        location.SetPreview(locationPreviewMetadataResult.Value);

        UnitResult<Error> saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            logger.LogError(
                "Cannot set location preview because save failed: {Error}",
                saveResult.Error.Message);

            return saveResult.Error.ToErrors();
        }

        return location.Id.Value;
    }
}
