using CSharpFunctionalExtensions;
using DirectoryService.Application.Locations;
using DirectoryService.Domain.Locations;
using Shared;

namespace DirectoryService.Infrastructure.Postgres.Repositories;

public class LocationsRepository(DirectoryServiceDbContext dbContext) : ILocationsRepository
{
    public async Task<Result<Guid, Error>> AddAsync(Location location, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Locations.AddAsync(location, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken); // Применяем изменения
        }
        catch (Exception e)
        {
            return Error.Failure(null, "Database error occurred.");
        }
        
        return Result.Success<Guid, Error>(location.Id.Value);
    }
}