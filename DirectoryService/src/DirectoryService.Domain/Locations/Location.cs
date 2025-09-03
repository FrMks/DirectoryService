using DirectoryService.Domain.Locations.ValueObjects;

namespace DirectoryService.Domain.Locations;

public class Location
{
    private Location(Guid id, Name name, Address address)
    {
        Id = id;
        Name = name;
        Address = address;
    }

    public Guid Id { get; private set; }

    public Name Name { get; private set; }
    
    public Address Address { get; private set; }
}