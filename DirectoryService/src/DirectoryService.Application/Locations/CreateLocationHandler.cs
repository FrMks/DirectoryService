using System.Net.Http.Headers;
using CSharpFunctionalExtensions;
using DirectoryService.Application.Locations.Fails.Exceptions;
using DirectoryService.Contracts.Locations;
using DirectoryService.Domain;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Locations.ValueObjects;
using Microsoft.Extensions.Logging;
using Shared;
using Errors = DirectoryService.Application.Locations.Fails.Errors;

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
        {
            // return locationNameResult.Error;
            return Errors.Locations.IncorrectCreationOfAClassNameInstance(locationNameResult.Error);
            // throw new IncorrectCreationOfAClassNameInstanceException();
            // throw new LocationValidationException([locationNameResult.Error]);
        }

        Name locationName = locationNameResult.Value;

        var locationAddressResult = Domain.Locations.ValueObjects.Address.Create(
            locationRequest.Address.Street,
            locationRequest.Address.City,
            locationRequest.Address.Country);
        if (locationAddressResult.IsFailure)
        {
            // return locationAddressResult.Error;
            return Errors.Locations.IncorrectCreationOfAClassAddressInstance(locationAddressResult.Error);
            // throw new IncorrectCreationOfAClassAddressInstanceException();
            // throw new LocationValidationException([locationNameResult.Error]);
        }

        var locationAddress = locationAddressResult.Value;

        var locationTimezoneResult = Timezone.Create(locationRequest.Timezone);
        if (locationTimezoneResult.IsFailure)
        {
            // return locationTimezoneResult.Error;
            return Errors.Locations.IncorrectCreationOfAClassTimezoneInstance(locationTimezoneResult.Error);
            // throw new IncorrectCreationOfAClassTimezoneInstanceException();
            // throw new LocationValidationException([locationNameResult.Error]);
        }

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