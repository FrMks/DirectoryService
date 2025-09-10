using System.Dynamic;
using CSharpFunctionalExtensions;
using DirectoryService.Domain.Locations;

namespace DirectoryService.Domain;

public class DepartmentLocation
{
    // EF Core
    private DepartmentLocation() { }
    
    private DepartmentLocation(Guid id, Guid departmentId, Guid locationId)
    {
        Id = id;
        DepartmentId = departmentId;
        LocationId = locationId;
    }

    public Guid Id { get; private set; }

    public Guid DepartmentId { get; private set; }

    public Guid LocationId { get; private set; }

    public static Result<DepartmentLocation> Create(Guid id, Guid departmentId, Guid locationId)
    {
        DepartmentLocation departmentLocation = new(id, departmentId, locationId);

        return Result.Success(departmentLocation);
    }

    public void SetId(Guid id) => Id = id;

    public void SetDepartmentId(Guid departmentId) => DepartmentId = departmentId;

    public void SetLocationId(Guid locationId) => LocationId = locationId;

}