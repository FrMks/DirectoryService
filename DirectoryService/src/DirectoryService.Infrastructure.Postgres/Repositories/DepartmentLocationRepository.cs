using DirectoryService.Application.Locations.Interfaces;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Infrastructure.Postgres.Repositories;

public class DepartmentLocationRepository(
    DirectoryServiceDbContext dbContext,
    ILogger<DepartmentLocationRepository> logger)
    : IDepartmentLocationRepository
{
    
}