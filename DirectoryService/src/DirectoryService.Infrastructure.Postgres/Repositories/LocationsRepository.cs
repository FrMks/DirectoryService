using CSharpFunctionalExtensions;
using DirectoryService.Application.Locations;
using DirectoryService.Domain.Locations;

namespace DirectoryService.Infrastructure.Postgres.Repositories;

public class LocationsRepository(DirectoryServiceDbContext dbContext) : ILocationsRepository
{
    public async Task<Result<Guid>> AddAsync(Location location, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Locations.AddAsync(location, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken); // Применяем изменения
        }
        catch (Exception e)
        {
            return Result.Failure<Guid>("Database error occurred.");
        }
        
        return Result.Success(location.Id.Value);
    }
}