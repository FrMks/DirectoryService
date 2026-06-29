using CSharpFunctionalExtensions;
using DirectoryService.Application.Database;
using DirectoryService.Contracts.Locations.GetLocations;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Locations.ValueObjects;
using FileService.Communication;
using FileService.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Core.Abstractions;

namespace DirectoryService.Application.Locations;

public class GetLocationByIdHandler(
    IReadDbContext readDbContext,
    IFileCommunicationService fileCommunicationService,
    ILogger<GetLocationByIdHandler> logger)
    : IQueryHandler<GetLocationByIdQuery, Result<GetLocationResponse, Errors>>
{
    public async Task<Result<GetLocationResponse, Errors>> Handle(
        GetLocationByIdQuery query,
        CancellationToken cancellationToken)
    {
        Location? location = await readDbContext.LocationsRead
            .FirstOrDefaultAsync(l => l.Id == query.LocationId, cancellationToken);

        if (location is null)
        {
            logger.LogError("Problem for taking location by {locationid} in database", query.LocationId);
            return Error.NotFound(
                "location.not.found",
                $"Cannot find location by {query.LocationId} in locations database").ToErrors();
        }

        if (location.PreviewMetadata is null)
        {
            return new GetLocationResponse(
                location.Id.Value,
                location.Name.Value,
                location.Address.Street,
                location.Address.City,
                location.Address.Country,
                location.Timezone.Value,
                location.IsActive,
                location.CreatedAt,
                location.UpdatedAt,
                new LocationPreviewResponse(
                    null,
                    "NotAttached",
                    null,
                    null,
                    null,
                    null,
                    null));
        }

        LocationPreviewMetadata previewMetadata = location.PreviewMetadata;

        Result<FileResponse, Errors> fileResponseResult = await fileCommunicationService.GetMediaAssetByIdAsync(
            previewMetadata.AssetId.Value,
            cancellationToken);
        if (fileResponseResult.IsFailure)
        {
            logger.LogError(
                "Error when try get file response from File Service: {exception}",
                fileResponseResult.Error.First().Message);
            return new GetLocationResponse(
                location.Id.Value,
                location.Name.Value,
                location.Address.Street,
                location.Address.City,
                location.Address.Country,
                location.Timezone.Value,
                location.IsActive,
                location.CreatedAt,
                location.UpdatedAt,
                new LocationPreviewResponse(
                    previewMetadata.AssetId.Value,
                    "TemporarilyUnavailable",
                    previewMetadata.FileName,
                    previewMetadata.ContentType,
                    previewMetadata.Size,
                    null,
                    "File Service is temporarily unavailable"));
        }

        return new GetLocationResponse(
            location.Id.Value,
            location.Name.Value,
            location.Address.Street,
            location.Address.City,
            location.Address.Country,
            location.Timezone.Value,
            location.IsActive,
            location.CreatedAt,
            location.UpdatedAt,
            new LocationPreviewResponse(
                location.PreviewMetadata.AssetId.Value,
                "Available",
                location.PreviewMetadata.FileName,
                location.PreviewMetadata.ContentType,
                location.PreviewMetadata.Size,
                fileResponseResult.Value.ContentUrl,
                null));
    }
}