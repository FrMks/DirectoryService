using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Contracts.Locations;
using DirectoryService.Domain;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Locations.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared;
using Errors = DirectoryService.Application.Locations.Fails.Errors;

namespace DirectoryService.Application.Locations;

public class CreateLocationHandler(
    ILocationsRepository locationsRepository,
    IValidator<CreateLocationRequest> validator,
    ILogger<CreateLocationHandler> logger)
    : ICommandHandler<Guid, CreateLocationCommand>
{
    public async Task<Result<Guid, Shared.Errors>> Handle(CreateLocationCommand locationCommand, CancellationToken cancellationToken)
    {
        // Валидация DTO
        var validationResult = await validator.ValidateAsync(locationCommand.LocationRequest, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = new Shared.Errors(validationResult.Errors.Select(failure =>
                Error.Validation(failure.ErrorCode, failure.ErrorMessage, failure.PropertyName)));
            logger.LogInformation("Error when check validation of dto: {Errors}", errors);
            return Errors.Locations.IncorrectDtoValidator(errors);
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