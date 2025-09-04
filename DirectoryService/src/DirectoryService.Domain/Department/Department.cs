using DirectoryService.Domain.Department.ValueObject;

namespace DirectoryService.Domain.Department;

public class Department
{
    private Department(Guid id, Name name)
    {
        Id = id;
        Name = name;
    }
    
    public Guid Id { get; private set; }
    
    public Name Name { get; private set; }
}