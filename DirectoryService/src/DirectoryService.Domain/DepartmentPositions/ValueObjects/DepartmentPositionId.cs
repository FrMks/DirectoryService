namespace DirectoryService.Domain.DepartmentPositions.ValueObjects;

public record DepartmentPositionId
{
    private DepartmentPositionId(Guid value)
    {
        Value = value;
    }
    
    public Guid Value { get; }
    
    public static DepartmentPositionId NewDepartmentId() => new (Guid.NewGuid());
    
    public static DepartmentPositionId Empty() => new (Guid.Empty);
    
    public static DepartmentPositionId FromValue(Guid value) => new(value);
}