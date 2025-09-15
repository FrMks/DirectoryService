namespace DirectoryService.Domain.Positions.ValueObject;

public class PositionId
{
    private PositionId(Guid value)
    {
        Value = value;
    }
    
    public Guid Value { get; }
    
    public static PositionId NewDepartmentId() => new (Guid.NewGuid());
    
    public static PositionId Empty() => new (Guid.Empty);
    
    public static PositionId FromValue(Guid value) => new(value);
}