using CSharpFunctionalExtensions;
using DirectoryService.Application.Locations.Interfaces;
using DirectoryService.Domain.Locations;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Core.Abstractions;
using Shared.Core.Database;

namespace DirectoryService.Application.Locations;

public class RemoveLocationPreviewHandler(
    ILocationsRepository locationsRepository,
    ITransactionManager transactionManager,
    ILogger<RemoveLocationPreviewHandler> logger)
    : ICommandHandler<Guid, RemoveLocationPreviewCommand>
{
    public async Task<Result<Guid, Errors>> Handle(RemoveLocationPreviewCommand command, CancellationToken cancellationToken)
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
        location.RemovePreview();

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