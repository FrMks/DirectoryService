using DirectoryService.Domain.Locations.ValueObjects;

namespace DirectoryService.Domain.Locations;

public class Location
{
    // EF Core
    private Location() { }
    
    private Location(Guid id, Name name, Address address,
        Timezone timezone, IReadOnlyList<DepartmentLocation> departmentLocations)
    {
        Id = id;
        Name = name;
        Address = address;
        Timezone = timezone;
        DepartmentLocations = departmentLocations;
    }

    #region Properties

    public Guid Id { get; private set; }

    public Name Name { get; private set; } = null!;

    public Address Address { get; private set; } = null!;

    public Timezone Timezone { get; private set; } = null!;

    public bool IsActive { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    
    public DateTime UpdateAt { get; private set; }
    
    public IReadOnlyList<DepartmentLocation> DepartmentLocations { get; private set; } = null!;

    #endregion
    
    #region Public methods

    public void SetId(Guid id) => Id = id;
    public void SetName(Name name) => Name = name;
    public void SetAddress(Address address) => Address = address;
    public void SetTimezone(Timezone timezone) => Timezone = timezone;
    public void SetIsActive(bool isActive) => IsActive = isActive;
    public void SetCreatedAt(DateTime createdAt) => CreatedAt = createdAt;
    public void SetUpdateAt(DateTime updateAt) => UpdateAt = updateAt;
    public void SetDepartmentLocation(IReadOnlyList<DepartmentLocation> departmentLocations) => DepartmentLocations = departmentLocations; 

    #endregion
}