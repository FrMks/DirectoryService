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

        List<GetLocationsResponse> mappedLocations = new();
        
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

        if (locationsCommand.LocationsRequest.Search != null)
        {
            var mappedLocationsResult = mappedLocations.Any()
                ? FilterBySearchInMappedLocations(mappedLocations, locationsCommand)
                : await FilterBySearchInDatabase(locationsRepository, locationsCommand, cancellationToken);
            
            if (mappedLocationsResult.IsFailure)
                return mappedLocationsResult.Error.ToErrors();
            
            mappedLocations = mappedLocationsResult.Value;
        }

        if (locationsCommand.LocationsRequest.IsActive != null)
        {
            var mappedLocationsResult = mappedLocations.Any()
                ? FilterByIsActiveInMappedLocations(mappedLocations, locationsCommand)
                : await FilterByIsActiveInDatabase(locationsRepository, locationsCommand, cancellationToken);
            
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

        return Result.Success<List<GetLocationsResponse>, Error>(locations.Value
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
            }).ToList());
    }

    private Result<List<GetLocationsResponse>, Error> FilterBySearchInMappedLocations(
        List<GetLocationsResponse> locations,
        GetLocationsCommand locationsCommand)
    {
        var mappedLocations = locations
            .Where(lr => lr.Name == locationsCommand.LocationsRequest.Search)
            .ToList();

        if (mappedLocations is null)
        {
            logger.LogError("Location with name {name} not found in mapped locations", locationsCommand.LocationsRequest.Search);
            return Error.Failure(
                "location.dont.have.in.list",
                $"Location with name {locationsCommand.LocationsRequest.Search} not found in mapped locations");   
        }
        
        return Result.Success<List<GetLocationsResponse>, Error>(mappedLocations);
    }
    
    private async Task<Result<List<GetLocationsResponse>, Error>> FilterBySearchInDatabase(
        ILocationsRepository locationsRepository,
        GetLocationsCommand locationsCommand,
        CancellationToken cancellationToken)
    {
        var locationResult = await locationsRepository.GetLocationByName(
            locationsCommand.LocationsRequest.Search,
            cancellationToken);
        
        if (locationResult.IsFailure)
            return locationResult.Error;
        
        var locationResponse = new GetLocationsResponse
        {
            Id = locationResult.Value.Id.Value,
            Name = locationResult.Value.Name.Value,
            Street = locationResult.Value.Address.Street,
            City = locationResult.Value.Address.City,
            Country = locationResult.Value.Address.Country,
            Timezone = locationResult.Value.Timezone.Value,
            IsActive = locationResult.Value.IsActive,
            CreatedAt = locationResult.Value.CreatedAt,
            UpdatedAt = locationResult.Value.UpdatedAt
        };
        
        var listLocationResponse = new List<GetLocationsResponse>();
        listLocationResponse.Add(locationResponse);
        
        return Result.Success<List<GetLocationsResponse>, Error>(listLocationResponse);
    }

    private Result<List<GetLocationsResponse>, Error> FilterByIsActiveInMappedLocations(
        List<GetLocationsResponse> locations,
        GetLocationsCommand locationsCommand)
    {
        var mappedLocations = locations
            .Where(lr => lr.IsActive == locationsCommand.LocationsRequest.IsActive)
            .ToList();

        if (mappedLocations.Count == 0)
        {
            logger.LogError(
                "Location with isActive: {isActive} not found in mapped locations. And finish list have not elements",
                locationsCommand.LocationsRequest.IsActive);
            return Error.Failure(
                "location.dont.have.in.list",
                $"Location with isActive: {locationsCommand.LocationsRequest.IsActive} not found in mapped locations");
        }
        
        return Result.Success<List<GetLocationsResponse>, Error>(mappedLocations);
    }
    
    private async Task<Result<List<GetLocationsResponse>, Error>> FilterByIsActiveInDatabase(
        ILocationsRepository locationsRepository,
        GetLocationsCommand locationsCommand,
        CancellationToken cancellationToken)
    {
        var locationsResult = await locationsRepository.GetLocationsByIsActive(
            (bool)locationsCommand.LocationsRequest.IsActive,
            cancellationToken);
        
        if (locationsResult.IsFailure)
            return locationsResult.Error;
        
        return Result.Success<List<GetLocationsResponse>, Error>(locationsResult.Value
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
            }).ToList());
    }
}