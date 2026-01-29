using CSharpFunctionalExtensions;
using DirectoryService.Application.Database;
using DirectoryService.Application.Extensions;
using DirectoryService.Contracts.Locations.GetLocations;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Locations;

public class GetLocationsHandler(
    IReadDbContext readDbContext,
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
                readDbContext,
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
                : await FilterBySearchInDatabase(readDbContext, locationsQuery, cancellationToken);

            if (mappedLocationsResult.IsFailure)
                return mappedLocationsResult.Error.ToErrors();

            mappedLocations = mappedLocationsResult.Value;
        }

        if (locationsQuery.LocationsRequest.IsActive != null)
        {
            var mappedLocationsResult = mappedLocations.Any()
                ? FilterByIsActiveInMappedLocations(mappedLocations, locationsQuery)
                : await FilterByIsActiveInDatabase(readDbContext, locationsQuery, cancellationToken);

            if (mappedLocationsResult.IsFailure)
                return mappedLocationsResult.Error.ToErrors();

            mappedLocations = mappedLocationsResult.Value;
        }

        if (locationsQuery.LocationsRequest.PageSize != null &&
            locationsQuery.LocationsRequest.Page != null)
        {
            var mappedLocationsRequest = mappedLocations.Any()
                ? FilterByPaginationInMappedLocations(mappedLocations, locationsQuery)
                : await FilterByPaginationInDatabase(readDbContext, locationsQuery, cancellationToken);

            if (mappedLocationsRequest.IsFailure)
                return mappedLocationsRequest.Error.ToErrors();

            mappedLocations = mappedLocationsRequest.Value;
        }

        return mappedLocations;
    }

    private async Task<Result<List<GetLocationsResponse>, Error>> FilterByDepartmentIds(
        IReadDbContext readDbContext,
        GetLocationsQuery locationsCommand,
        CancellationToken cancellationToken)
    {
        if (locationsCommand.LocationsRequest.DepartmentIds.Any() == false)
        {
            logger.LogError("DepartmentIds are empty when searching locations by department ids");
            return Error.Failure(
                "departmentIds.are.empty",
                "departmentIds are empty when searching locations by department ids");
        }

        var locations = await readDbContext.DepartmentLocationsRead
            .Where(dl => locationsCommand.LocationsRequest.DepartmentIds.Contains(dl.DepartmentId))
            .Join(readDbContext.LocationsRead,
                dl => dl.LocationId,
                l => l.Id,
                (dl, l) => l)
            .Distinct() // Без дубликатов
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
                UpdatedAt = l.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        if (locations.Count == 0)
        {
            logger.LogError("No locations found for the given department ids");
            return Error.NotFound(
                "location.dont.have.in.db",
                "No locations found for the given department ids",
                null);
        }

        return Result.Success<List<GetLocationsResponse>, Error>(locations);
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
        GetLocationsQuery locationsQuery,
        CancellationToken cancellationToken)
    {
        var location = await readDbContext.LocationsRead
            .Where(l => EF.Functions.ILike(l.Name.Value, $"%{locationsQuery.LocationsRequest.Search}%"))
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
                UpdatedAt = l.UpdatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (location is null)
        {
            logger.LogError(
                "Location with name {name} not found in database.",
                locationsQuery.LocationsRequest.Search);
            return Error.NotFound(
                "location.not.found",
                $"Location with name {locationsQuery.LocationsRequest.Search} not found",
                null);
        }

        return Result.Success<List<GetLocationsResponse>, Error>(new List<GetLocationsResponse> { location });
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
        IReadDbContext readDbContext,
        GetLocationsQuery locationsQuery,
        CancellationToken cancellationToken)
    {
        var locations = await readDbContext.LocationsRead
            .Where(l => l.IsActive == locationsQuery.LocationsRequest.IsActive)
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
                UpdatedAt = l.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        if (locations is null)
        {
            logger.LogError("Locations are empty when searching locations by is active");
            return Error.NotFound(
                "location.dont.have.in.db",
                "Locations are empty when searching locations by is active",
                null);
        }

        return Result.Success<List<GetLocationsResponse>, Error>(locations);
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
        IReadDbContext readDbContext,
        GetLocationsQuery locationsQuery,
        CancellationToken cancellationToken)
    {
        if (locationsQuery.LocationsRequest.Page is null)
        {
            logger.LogError("In location request PAGE is null");
            return Error.Failure("location.request.page", "In location request PAGE is null");
        }

        if (locationsQuery.LocationsRequest.PageSize is null)
        {
            logger.LogError("In location request PAGE SIZE is null");
            return Error.Failure("location.request.page", "In location request PAGE SIZE is null");
        }

        int page = locationsQuery.LocationsRequest.Page < 1
            ? 1 : (int)locationsQuery.LocationsRequest.Page;

        int pageSize = locationsQuery.LocationsRequest.PageSize < 1
            ? 1 : (int)locationsQuery.LocationsRequest.PageSize;

        int skipCount = (page - 1) * pageSize;

        var locations = await readDbContext.LocationsRead
            .OrderBy(l => l.Name) // TODO: Я подумал, что сначала надо сделать сортировку по имени, так надо?
            .Skip(skipCount)
            .Take(pageSize)
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
            })
            .ToListAsync(cancellationToken);

        if (locations is null)
        {
            logger.LogError("Locations are empty when searching locations by pagination");
            return Error.NotFound(
                "location.dont.have.in.db",
                "Locations are empty when searching locations by pagination",
                null);
        }

        return Result.Success<List<GetLocationsResponse>, Error>(locations);
    }
}