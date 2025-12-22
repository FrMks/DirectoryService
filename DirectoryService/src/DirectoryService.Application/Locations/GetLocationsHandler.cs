using CSharpFunctionalExtensions;
using DirectoryService.Application.Extensions;
using DirectoryService.Application.Locations.Interfaces;
using DirectoryService.Contracts.Locations.GetLocations;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Locations;

public class GetLocationsHandler(
    ILocationsRepository locationsRepository,
    IValidator<GetLocationsRequest> validator,
    ILogger<CreateLocationHandler> logger)
{
    public async Task<Result<GetLocationsResponse, Errors>> Handle(GetLocationsCommand locationsCommand, CancellationToken cancellationToken)
    {
        // Валидация DTO
        var validationResult = await validator.ValidateAsync(locationsCommand.LocationsRequest, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.ToList())
            {
                logger.LogInformation("Error when creating location, error: {error}", error.Message);
            }

            return validationResult.ToList();
        }
        
        return Result.Success<GetLocationsResponse, Errors>(new GetLocationsResponse());
    }
}