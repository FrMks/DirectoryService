using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using DirectoryService.Application.Locations.Interfaces;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Locations.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Infrastructure.Postgres.Repositories;

public class LocationsRepository(DirectoryServiceDbContext dbContext, ILogger<LocationsRepository> logger)
    : ILocationsRepository
{
    public async Task<Result<Guid, Error>> AddAsync(Location location, CancellationToken cancellationToken)
    {
        try
        {
            var haveLocationInDatabaseWithSameLocation = await dbContext.Locations.
                AnyAsync(l => l.Address.Street == location.Address.Street
                              && l.Address.City == location.Address.City &&
                              l.Address.Country == location.Address.Country);

            if (haveLocationInDatabaseWithSameLocation)
            {
                logger.LogInformation(
                    "Address with {street} and {city} and {country} already exists",
                    location.Address.Street, location.Address.City, location.Address.Country);
                return Error.Failure(null, "Address already exists in database.");
            }

            await dbContext.Locations.AddAsync(location, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken); // Применяем изменения

            logger.LogInformation("Successfully added to the database with id{location}", location.Id.Value);
        }
        catch (Exception e)
        {
            return Error.Failure(
                null,
                "Database error occurred when added location to a database.");
        }

        return Result.Success<Guid, Error>(location.Id.Value);
    }

    public async Task<Result<bool, Error>> AllExistAsync(List<Guid> locationIds, CancellationToken cancellationToken)
    {
        var locations = await dbContext.Locations
            .Where(l => locationIds.Contains(l.Id))
            .ToListAsync(cancellationToken);

        if (locations.Count != locationIds.Count)
        {
            return Error.Failure(
                "location.not.found",
                $"Some location id does not have in database");
        }

        return true;
    }

    public async Task<Result<Location, Error>> GetBy(
        Expression<Func<Location, bool>> predicate,
        CancellationToken cancellationToken)
    {
        var location = await dbContext.Locations.FirstOrDefaultAsync(predicate, cancellationToken);

        if (location is null)
        {
            logger.LogError("Location not found with given predicate");
            return Error.NotFound(
                "location.not.found",
                $"Location not found.",
                null);
        }

        return location;
    }

    public async Task<Result<List<Location>, Error>> GetLocationsByIds(
        List<LocationId> locationIds,
        CancellationToken cancellationToken)
    {
        var locations = await dbContext.Locations
            .Where(l => locationIds.Contains(l.Id))
            .ToListAsync(cancellationToken);

        if (locations is null || locations.Count == 0)
        {
            logger.LogError("Not all locations found with the given IDs");
            return Error.NotFound(
                "locations.not.found",
                "Not all locations found with the given IDs.",
                null);
        }

        return locations;
    }

    public async Task<Result<HashSet<LocationId>, Error>> GetLocationIdsWithOtherActiveDepartments(
        List<LocationId> locationIds,
        DepartmentId deletingDepartmentId,
        CancellationToken cancellationToken)
    {
        var existingLocationIds = await dbContext.Locations
            .Where(l => locationIds.Contains(l.Id))
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        if (existingLocationIds is null || existingLocationIds.Count == 0)
        {
            logger.LogError("Not all locations found with the given IDs");
            return Error.NotFound(
                "locations.not.found",
                "Not all locations found with the given IDs.",
                null);
        }

        var locationIdsWithOtherActiveDepartments = await dbContext.DepartmentLocations
            .Join(
                dbContext.Departments,
                dl => dl.DepartmentId,
                d => d.Id,
                (dl, d) => new { DepartmentLocation = dl, Department = d })
            .Where(
                x => locationIds.Contains(x.DepartmentLocation.LocationId) &&
                     x.DepartmentLocation.DepartmentId != deletingDepartmentId &&
                     x.Department.IsActive)
            .Select(x => x.DepartmentLocation.LocationId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return locationIdsWithOtherActiveDepartments.ToHashSet();
    }
}
