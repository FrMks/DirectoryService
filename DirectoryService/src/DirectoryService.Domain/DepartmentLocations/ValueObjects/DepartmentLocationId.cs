namespace DirectoryService.Domain.ValueObjects;

public class DepartmentLocationId
{
    private DepartmentLocationId(Guid value)
    {
        Value = value;
    }
    
    public Guid Value { get; }
    
    public static DepartmentLocationId NewDepartmentId() => new (Guid.NewGuid());
    
    public static DepartmentLocationId Empty() => new (Guid.Empty);
    
    public static DepartmentLocationId FromValue(Guid value) => new(value);
}