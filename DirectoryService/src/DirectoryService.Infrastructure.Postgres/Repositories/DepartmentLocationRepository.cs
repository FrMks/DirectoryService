using DirectoryService.Application.DepartmentLocation.Interfaces;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Infrastructure.Postgres.Repositories;

public class DepartmentLocationRepository(
    DirectoryServiceDbContext dbContext,
    ILogger<DepartmentLocationRepository> logger)
    : IDepartmentLocationRepository
{
    
}