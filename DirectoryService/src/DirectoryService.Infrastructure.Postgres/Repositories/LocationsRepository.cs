using DirectoryService.Application.Locations;
using DirectoryService.Domain.Locations;

namespace DirectoryService.Infrastructure.Postgres.Repositories;

public class LocationsRepository(DirectoryServiceDbContext dbContext) : ILocationsRepository
{
    public async Task<Guid> AddAsync(Location location, CancellationToken cancellationToken)
    {
        await dbContext.Locations.AddAsync(location, cancellationToken);
        
        await dbContext.SaveChangesAsync(cancellationToken); // Применяем изменения
        
        return location.Id.Value;
    }
}