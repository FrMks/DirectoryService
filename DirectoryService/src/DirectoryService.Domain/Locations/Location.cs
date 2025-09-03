using DirectoryService.Domain.Locations;

namespace DirectoryService.Domain;

public class Location
{
    private Location() { }

    private Location(Guid id, Name name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; private set; }
    
    public Name Name { get; private set; }
}