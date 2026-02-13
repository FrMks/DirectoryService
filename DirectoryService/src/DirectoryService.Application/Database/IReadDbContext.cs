using DirectoryService.Domain.Department;
using DirectoryService.Domain.Locations;
using DepartmentLocationEntity = DirectoryService.Domain.DepartmentLocations.DepartmentLocation;

namespace DirectoryService.Application.Database;

public interface IReadDbContext
{
    IQueryable<Location> LocationsRead { get; }

    IQueryable<DepartmentLocationEntity> DepartmentLocationsRead { get; }

    IQueryable<Department> DepartmentsRead { get; }
}