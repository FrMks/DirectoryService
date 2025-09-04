using System.Dynamic;
using CSharpFunctionalExtensions;
using DirectoryService.Domain.Locations;

namespace DirectoryService.Domain;

public class DepartmentLocation
{
    private DepartmentLocation(Guid id, Guid departmentId,
        Guid locationId, Location location, Department.Department department)
    {
        Id = id;
        DepartmentId = departmentId;
        LocationId = locationId;
        Location = location;
        Department = department;
    }

    public Guid Id { get; private set; }

    public Guid DepartmentId { get; private set; }

    public Guid LocationId { get; private set; }

    public Location Location { get; private set; }

    public Department.Department Department { get; private set; }

    public static Result<DepartmentLocation> Create(Guid id, Guid departmentId,
        Guid locationId, Location location, Department.Department department)
    {
        DepartmentLocation departmentLocation = new DepartmentLocation(
            id, departmentId, locationId, location, department);

        return Result.Success(departmentLocation);
    }

    public void SetId(Guid id) => Id = id;

    public void SetDepartmentId(Guid departmentId) => DepartmentId = departmentId;

    public void SetLocationId(Guid locationId) => LocationId = locationId;

    public void SetLocation(Location location) => Location = location;

    public void SetDepartment(Department.Department department) => Department = department;
}