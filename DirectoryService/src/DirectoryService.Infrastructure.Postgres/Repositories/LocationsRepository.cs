using CSharpFunctionalExtensions;
using DirectoryService.Application.Locations.Interfaces;
using DirectoryService.Domain.Locations;
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

    public async Task<Result<List<Location>, Error>> GetLocationsAsync(
        List<Guid> locationIds,
        CancellationToken cancellationToken)
    {
        if (locationIds.Any() == false)
        {
            logger.LogError("LocationIds are empty when searching locations by location ids");
            return Error.Failure(
                "locationIds.are.empty",
                "LocationIds are empty when searching locations by location ids");
        }

        var locations = await dbContext.Locations
            .AsNoTracking()
            .Where(l => locationIds.Contains(l.Id))
            .ToListAsync(cancellationToken);

        if (locations.Count != locationIds.Count)
        {
            logger.LogError("LocationIds are not the same number of locations");
            return Error.Failure(
                "some.locationIds.dont.have.in.db",
                "some locationIds dont have in Locations db");
        }

        return locations;
    }
}