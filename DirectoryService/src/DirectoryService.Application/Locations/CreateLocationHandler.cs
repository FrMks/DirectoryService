using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Locations;
using DirectoryService.Domain;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Locations.ValueObjects;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Locations;

public class CreateLocationHandler(
    ILocationsRepository locationsRepository,
    ILogger<CreateLocationHandler> logger)
    : ICreateLocationHandler
{
    public async Task<Result<Guid, Error>> Handle(CreateLocationRequest locationRequest, CancellationToken cancellationToken)
    {
        // Создание сущности Location
        LocationId locationId = LocationId.NewLocationId();
        
        var locationNameResult = Name.Create(locationRequest.Name);
        if (locationNameResult.IsFailure)
            return locationNameResult.Error;
        Name locationName = locationNameResult.Value;

        var locationAddressResult = Domain.Locations.ValueObjects.Address.Create(
            locationRequest.Address.Street,
            locationRequest.Address.City,
            locationRequest.Address.Country);
        
        if (locationAddressResult.IsFailure)
            return locationAddressResult.Error;
            // throw new LocationValidationException(locationAddressResult.Error.ToString());
                
        var locationAddress = locationAddressResult.Value;
        
        var locationTimezoneResult = Timezone.Create(locationRequest.Timezone);
        if (locationTimezoneResult.IsFailure)
            return locationTimezoneResult.Error;
        Timezone locationTimezone = locationTimezoneResult.Value;

        Location location = Location.Create(locationId, locationName,
            locationAddress, locationTimezone,
            new List<DepartmentLocation>()).Value;

        // Сохранение сущность Location в БД
        var successfulId = await locationsRepository.AddAsync(location, cancellationToken);

        // Логирование об успешном или неуспешном сохранении
        logger.LogInformation("Creating location with id {successfulId.Value}", successfulId.Value);

        return locationId.Value;
    }
}