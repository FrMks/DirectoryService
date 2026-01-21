using CSharpFunctionalExtensions;
using DirectoryService.Application.Database;
using DirectoryService.Application.DepartmentLocation.Interfaces;
using DirectoryService.Application.Extensions;
using DirectoryService.Application.Locations.Interfaces;
using DirectoryService.Contracts.Locations.GetLocations;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Locations;

public class GetLocationsHandler(
    IReadDbContext readDbContext,
    ILocationsRepository locationsRepository,
    IDepartmentLocationRepository departmentLocationRepository,
    IValidator<GetLocationsRequest> validator,
    ILogger<CreateLocationHandler> logger)
{
    public async Task<Result<List<GetLocationsResponse>, Errors>> Handle(
        GetLocationsQuery locationsQuery,
        CancellationToken cancellationToken)
    {
        // Валидация DTO
        var validationResult = await validator.ValidateAsync(locationsQuery.LocationsRequest, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.ToList())
            {
                logger.LogInformation("Error when creating location, error: {error}", error.Message);
            }

            return validationResult.ToList();
        }

        List<GetLocationsResponse> mappedLocations = new();

        if (locationsQuery.LocationsRequest.DepartmentIds.Count != 0)
        {
            var mappedLocationsResult = await FilterByDepartmentIds(
                departmentLocationRepository,
                locationsQuery,
                cancellationToken);

            if (mappedLocationsResult.IsFailure)
                return mappedLocationsResult.Error.ToErrors();

            mappedLocations = mappedLocationsResult.Value;
        }

        if (locationsQuery.LocationsRequest.Search != null)
        {
            var mappedLocationsResult = mappedLocations.Any()
                ? FilterBySearchInMappedLocations(mappedLocations, locationsQuery)
                : await FilterBySearchInDatabase(readDbContext, locationsRepository, locationsQuery, cancellationToken);

            if (mappedLocationsResult.IsFailure)
                return mappedLocationsResult.Error.ToErrors();

            mappedLocations = mappedLocationsResult.Value;
        }

        if (locationsQuery.LocationsRequest.IsActive != null)
        {
            var mappedLocationsResult = mappedLocations.Any()
                ? FilterByIsActiveInMappedLocations(mappedLocations, locationsQuery)
                : await FilterByIsActiveInDatabase(locationsRepository, locationsQuery, cancellationToken);

            if (mappedLocationsResult.IsFailure)
                return mappedLocationsResult.Error.ToErrors();

            mappedLocations = mappedLocationsResult.Value;
        }

        if (locationsQuery.LocationsRequest.PageSize != null &&
            locationsQuery.LocationsRequest.Page != null)
        {
            var mappedLocationsRequest = mappedLocations.Any()
                ? FilterByPaginationInMappedLocations(mappedLocations, locationsQuery)
                : await FilterByPaginationInDatabase(locationsRepository, locationsQuery, cancellationToken);

            if (mappedLocationsRequest.IsFailure)
                return mappedLocationsRequest.Error.ToErrors();

            mappedLocations = mappedLocationsRequest.Value;
        }

        return mappedLocations;
    }

    private async Task<Result<List<GetLocationsResponse>, Error>> FilterByDepartmentIds(
        IDepartmentLocationRepository departmentLocationRepository,
        GetLocationsQuery locationsCommand,
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
        GetLocationsQuery locationsCommand)
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
        IReadDbContext readDbContext,
        ILocationsRepository locationsRepository,
        GetLocationsQuery locationsCommand,
        CancellationToken cancellationToken)
    {
        // TODO: перенести логику из репозиториев сюда
        // var location = readDbContext.LocationsRead
        // if (Location is null)
        // {
        //     return null;
        // }

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
        GetLocationsQuery locationsCommand)
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
        GetLocationsQuery locationsCommand,
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

    private Result<List<GetLocationsResponse>, Error> FilterByPaginationInMappedLocations(
        List<GetLocationsResponse> locations,
        GetLocationsQuery locationsCommand)
    {
        if (locationsCommand.LocationsRequest.PageSize < 1)
        {
            logger.LogError("Request with page size < 1 cannot be executed. Page size must be >= 1");
        }

        if (locationsCommand.LocationsRequest.Page < 1)
        {
            logger.LogError("Request with page number < 1 cannot be executed. Page number must be >= 1");
        }

        int skipCount = (int)((locationsCommand.LocationsRequest.PageSize - 1) * locationsCommand.LocationsRequest.PageSize);
        var mappedLocations = locations
            .Skip(skipCount)
            .Take((int)locationsCommand.LocationsRequest.PageSize)
            .ToList();

        if (mappedLocations.Count == 0)
        {
            logger.LogError(
                "Location with page size {pageSize} and page count {pageCount} not found in mapped locations." +
                " And finish list have not elements",
                locationsCommand.LocationsRequest.PageSize, locationsCommand.LocationsRequest.Page);
            return Error.Failure(
                "location.dont.have.in.list",
                $"Location with page size {locationsCommand.LocationsRequest.PageSize} and" +
                $" page count {locationsCommand.LocationsRequest.Page} not found in mapped locations");
        }

        return Result.Success<List<GetLocationsResponse>, Error>(mappedLocations);
    }

    private async Task<Result<List<GetLocationsResponse>, Error>> FilterByPaginationInDatabase(
        ILocationsRepository locationsRepository,
        GetLocationsQuery locationsCommand,
        CancellationToken cancellationToken)
    {
        var locationsResult = await locationsRepository.GetLocationsByPagination(
            (int)locationsCommand.LocationsRequest.Page,
            (int)locationsCommand.LocationsRequest.PageSize,
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