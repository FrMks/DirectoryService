using CSharpFunctionalExtensions;
using DirectoryService.Domain.Department.ValueObject;

namespace DirectoryService.Domain.Department;

public class Department
{
    private Department(Name name, Identifier identifier)
    {
        Id = Guid.NewGuid();
        Name = name;
        Identifier = identifier;
    }
    
    public Guid Id { get; private set; }
    
    public Name Name { get; private set; }
    
    public Identifier Identifier { get; private set; }
}