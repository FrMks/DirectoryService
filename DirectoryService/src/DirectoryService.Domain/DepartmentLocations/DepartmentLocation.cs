using System.Dynamic;
using CSharpFunctionalExtensions;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Locations.ValueObjects;

namespace DirectoryService.Domain;

public class DepartmentLocation
{
    // EF Core
    private DepartmentLocation() { }
    
    private DepartmentLocation(Guid id, DepartmentId departmentId, LocationId locationId)
    {
        Id = id;
        DepartmentId = departmentId;
        LocationId = locationId;
    }

    public Guid Id { get; private set; }

    public DepartmentId DepartmentId { get; private set; }

    public LocationId LocationId { get; private set; }

    public static Result<DepartmentLocation> Create(Guid id, DepartmentId departmentId, LocationId locationId)
    {
        DepartmentLocation departmentLocation = new(id, departmentId, locationId);

        return Result.Success(departmentLocation);
    }

    public void SetId(Guid id) => Id = id;

    public void SetDepartmentId(DepartmentId departmentId) => DepartmentId = departmentId;

    public void SetLocationId(LocationId locationId) => LocationId = locationId;

}