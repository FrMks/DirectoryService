﻿using CSharpFunctionalExtensions;
using DirectoryService.Domain.Locations.ValueObjects;
using Name = DirectoryService.Domain.Locations.ValueObjects.Name;

namespace DirectoryService.Domain.Locations;

public sealed class Location
{
    // EF Core
    private Location() { }
    
    private Location(LocationId id, Name name, Address address,
        Timezone timezone, bool isActive,
        DateTime createdAt, DateTime updatedAt,
        IEnumerable<DepartmentLocation> departmentLocations)
    {
        Id = id;
        Name = name;
        Address = address;
        Timezone = timezone;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        DepartmentLocations = departmentLocations.ToList();
    }

    #region Properties

    public LocationId Id { get; private set; }

    public Name Name { get; private set; } = null!;

    public Address Address { get; private set; } = null!;

    public Timezone Timezone { get; private set; } = null!;

    public bool IsActive { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    
    public DateTime UpdatedAt { get; private set; }
    
    public IReadOnlyList<DepartmentLocation> DepartmentLocations { get; private set; } = null!;

    #endregion
    
    public static Result<Location> Create(LocationId id, Name name, Address address,
        Timezone timezone, bool isActive,
        DateTime createdAt, DateTime updatedAt,
        IEnumerable<DepartmentLocation> departmentLocations)
    {
        Location location = new(id, name, address, timezone, isActive, createdAt, updatedAt, departmentLocations);
        
        return Result.Success(location);
    }
}