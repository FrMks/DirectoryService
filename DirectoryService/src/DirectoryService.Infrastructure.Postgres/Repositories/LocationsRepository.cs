using CSharpFunctionalExtensions;
using DirectoryService.Application.Locations;
using DirectoryService.Application.Locations.Interfaces;
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
            
            logger.LogInformation("Successfully added to the database with {location}", location.Id.Value);
        }
        catch (Exception e)
        {
            return Error.Failure(null, "Database error occurred.");
        }
        
        return Result.Success<Guid, Error>(location.Id.Value);
    }
    
    public async Task<Result<bool, Error>> AllExistAsync(List<Guid> locationIds, CancellationToken cancellationToken)
    {
        foreach (var locationId in locationIds)
        {
            var location = await dbContext.Locations.FirstOrDefaultAsync(l => l.Id == LocationId.FromValue(locationId), cancellationToken);

            if (location is null)
                return Error.NotFound("location.not.found", $"Location with id: {locationId} not found.", locationId);
        }
        
        return true;
    }
}