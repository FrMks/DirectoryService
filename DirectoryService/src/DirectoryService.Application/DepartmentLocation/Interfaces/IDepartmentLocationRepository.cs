using CSharpFunctionalExtensions;
using DirectoryService.Domain.Locations.ValueObjects;
using Shared;

namespace DirectoryService.Application.DepartmentLocation.Interfaces;

public interface IDepartmentLocationRepository
{
    public Task<Result<List<LocationId>, Error>> GetLocationIdsAsync(List<Guid> departmentIds);
}