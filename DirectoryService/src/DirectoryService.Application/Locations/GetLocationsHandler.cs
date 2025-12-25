using System.Net;
using CSharpFunctionalExtensions;
using DirectoryService.Application.DepartmentLocation.Interfaces;
using DirectoryService.Application.Extensions;
using DirectoryService.Application.Locations.Interfaces;
using DirectoryService.Contracts.Locations.GetLocations;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Locations;

public class GetLocationsHandler(
    ILocationsRepository locationsRepository,
    IDepartmentLocationRepository departmentLocationRepository,
    IValidator<GetLocationsRequest> validator,
    ILogger<CreateLocationHandler> logger)
{
    public async Task<Result<List<GetLocationsResponse>, Errors>> Handle(
        GetLocationsCommand locationsCommand,
        CancellationToken cancellationToken)
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

        List<GetLocationsResponse> mappedLocations;
        
        if (locationsCommand.LocationsRequest.DepartmentIds.Count != 0)
        {
            var mappedLocationsResult = await FilterByDepartmentIds(
                departmentLocationRepository,
                locationsCommand,
                cancellationToken);

            if (mappedLocationsResult.IsFailure)
                return mappedLocationsResult.Error.ToErrors();
            
            mappedLocations = mappedLocationsResult.Value;
        }
        
        return Result.Success<List<GetLocationsResponse>, Errors>(new List<GetLocationsResponse>());
    }

    private async Task<Result<List<GetLocationsResponse>, Error>> FilterByDepartmentIds(
        IDepartmentLocationRepository departmentLocationRepository,
        GetLocationsCommand locationsCommand,
        CancellationToken cancellationToken)
    {
        var locationIds = await departmentLocationRepository.GetLocationIdsAsync(
            locationsCommand.LocationsRequest.DepartmentIds,
            cancellationToken);

        if (locationIds.IsFailure)
            return locationIds.Error;

        var guidLocationIds = locationIds.Value
            .Select(d => d.Value)
            .ToList();
            
        var locations = await locationsRepository.GetLocationsAsync(
            guidLocationIds,
            cancellationToken);
            
        if (locations.IsFailure)
            return locations.Error;

        return locations.Value
            .Select(l => new GetLocationsResponse
            {
                Id = l.Id.Value,
                Name = l.Name.Value,
                Street = l.Address.Street,
                City = l.Address.City,
                Country = l.Address.Country,
                Timezone = l.Timezone.Value,
                IsActive = l.IsActive,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            }).ToList();
    }
}