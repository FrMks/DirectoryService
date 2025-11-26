namespace DirectoryService.Domain.Department.ValueObject;

public record DepartmentId
{
    private DepartmentId(Guid value)
    {
        Value = value;
    }
    
    public Guid Value { get; }
    
    public static DepartmentId NewDepartmentId() => new (Guid.NewGuid());
    
    public static DepartmentId Empty() => new (Guid.Empty);
    
    public static DepartmentId FromValue(Guid value) => new(value);
    
    public static implicit operator Guid(DepartmentId departmentId) => departmentId.Value;
}