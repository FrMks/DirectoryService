namespace DirectoryService.Domain.Locations.ValueObjects;

public class LocationId
{
    private LocationId(Guid value)
    {
        Value = value;
    }
    
    public Guid Value { get; }
    
    public static LocationId NewLocationId() => new (Guid.NewGuid());
    
    public static LocationId Empty() => new (Guid.Empty);
    
    public static LocationId FromValue(Guid value) => new(value);
    
    public static implicit operator Guid(LocationId locationId) => locationId.Value;
}