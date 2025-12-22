using CSharpFunctionalExtensions;
using DirectoryService.Application.DepartmentLocation.Interfaces;
using DirectoryService.Domain.Locations.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Infrastructure.Postgres.Repositories;

public class DepartmentLocationRepository(
    DirectoryServiceDbContext dbContext,
    ILogger<DepartmentLocationRepository> logger)
    : IDepartmentLocationRepository
{
    public async Task<Result<List<LocationId>, Error>> GetLocationIdsAsync(List<Guid?> departmentIds,
        CancellationToken cancellationToken)
    {
        if (departmentIds.Any() == false)
        {
            logger.LogError("DepartmentIds are empty when searching locations by department ids");
            return Error.Failure(
                "departmentIds.are.empty", 
                "departmentIds are empty when searching locations by department ids");   
        }

        var departmentLocations = await dbContext.DepartmentLocations
            .Where(d => departmentIds.Contains(d.DepartmentId))
            .ToListAsync(cancellationToken);

        if (departmentLocations.Count != departmentIds.Count)
        {
            logger.LogError("DepartmentIds are not the same number of department locations");
            return Error.Failure(
                "some.departmentIds.dont.have.in.db",
                "some departmentIds dont have in DepartmentLocations db");
        }
        
        var locationIds = departmentLocations
            .Select(dl => dl.LocationId)
            .Distinct() // Без дубликатов
            .ToList();
        
        return locationIds;
    }
}