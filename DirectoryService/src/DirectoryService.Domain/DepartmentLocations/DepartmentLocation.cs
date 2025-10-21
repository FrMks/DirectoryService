using CSharpFunctionalExtensions;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.Locations.ValueObjects;
using DirectoryService.Domain.ValueObjects;

namespace DirectoryService.Domain;

public sealed class DepartmentLocation
{
    // EF Core
    private DepartmentLocation() { }
    
    private DepartmentLocation(DepartmentLocationId id, DepartmentId departmentId, LocationId locationId)
    {
        Id = id;
        DepartmentId = departmentId;
        LocationId = locationId;
    }

    public DepartmentLocationId Id { get; private set; }

    public DepartmentId DepartmentId { get; private set; }

    public LocationId LocationId { get; private set; }

    public static Result<DepartmentLocation> Create(DepartmentLocationId id, DepartmentId departmentId, LocationId locationId)
    {
        DepartmentLocation departmentLocation = new(id, departmentId, locationId);

        return Result.Success(departmentLocation);
    }

    public void SetId(DepartmentLocationId id) => Id = id;

    public void SetDepartmentId(DepartmentId departmentId) => DepartmentId = departmentId;

    public void SetLocationId(LocationId locationId) => LocationId = locationId;

}