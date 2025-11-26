using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Extensions;
using DirectoryService.Application.Locations.Interfaces;
using DirectoryService.Contracts.Locations;
using DirectoryService.Domain;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Locations.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Locations;

public class CreateLocationHandler(
    ILocationsRepository locationsRepository,
    IValidator<CreateLocationRequest> validator,
    ILogger<CreateLocationHandler> logger)
    : ICommandHandler<Guid, CreateLocationCommand>
{
    public async Task<Result<Guid, Errors>> Handle(CreateLocationCommand locationCommand, CancellationToken cancellationToken)
    {
        // Валидация DTO
        var validationResult = await validator.ValidateAsync(locationCommand.LocationRequest, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.ToList())
            {
                logger.LogInformation("Error when creating location, error: {error}", error.Message);
            }

            return validationResult.ToList();
        }

        // Создание сущности Location
        LocationId locationId = LocationId.NewLocationId();

        var locationNameResult = Name.Create(locationCommand.LocationRequest.Name);
        Name locationName = locationNameResult.Value;

        var locationAddressResult = Domain.Locations.ValueObjects.Address.Create(
            locationCommand.LocationRequest.Address.Street,
            locationCommand.LocationRequest.Address.City,
            locationCommand.LocationRequest.Address.Country);
        var locationAddress = locationAddressResult.Value;

        var locationTimezoneResult = Timezone.Create(locationCommand.LocationRequest.Timezone);
        Timezone locationTimezone = locationTimezoneResult.Value;

        Location location = Location.Create(locationId, locationName,
            locationAddress, locationTimezone,
            new List<DepartmentLocation>()).Value;

        logger.LogInformation("Creating location with id {successfulId.Value}", location.Name);

        // Сохранение сущность Location в БД
        var successfulId = await locationsRepository.AddAsync(location, cancellationToken);

        if (successfulId.IsFailure)
            return Error.Failure(null, successfulId.Error.Message).ToErrors();
        
        // Логирование об успешном или неуспешном сохранении
        logger.LogInformation("Location with id {successfulId.Value} add to db.", successfulId.Value);

        return locationId.Value;
    }
}