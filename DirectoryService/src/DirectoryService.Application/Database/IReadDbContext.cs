using DirectoryService.Domain.Locations;

namespace DirectoryService.Application.Database;

public interface IReadDbContext
{
    IQueryable<Location> LocationsRead { get; }
}