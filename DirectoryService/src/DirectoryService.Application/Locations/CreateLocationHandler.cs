using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Locations;
using DirectoryService.Domain;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Locations.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Application.Locations;

public class CreateLocationHandler(
    ILocationsRepository locationsRepository,
    ILogger<CreateLocationHandler> logger)
    : ICreateLocationHandler
{
    // public async Task<Result<Guid>> Handle(CreateLocationRequest locationRequest, CancellationToken cancellationToken)
    public async Task<Result<Guid>> Handle(CreateLocationRequest locationRequest, CancellationToken cancellationToken)
    {
        // Создание сущности Location
        LocationId locationId = LocationId.NewLocationId();
        Name locationName = Name.Create(locationRequest.Name).Value;
        var locationAddress = Domain.Locations.ValueObjects.Address.Create(locationRequest.Address.Street, locationRequest.Address.City, locationRequest.Address.Country).Value;
        Timezone locationTimezone = Timezone.Create(locationRequest.Timezone).Value;
        
        Location location = Location.Create(locationId, locationName,
            locationAddress, locationTimezone,
            true, DateTime.UtcNow, DateTime.UtcNow,
            new List<DepartmentLocation>()).Value;

        // Сохранение сущность Location в БД
        await locationsRepository.AddAsync(location, cancellationToken);

        // Логирование об успешном или неуспешном сохранении
        logger.LogInformation("Creating location with id {LocationId}", locationId);
        
        return locationId.Value;
    }
}