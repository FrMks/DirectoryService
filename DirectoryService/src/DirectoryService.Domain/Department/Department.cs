using DirectoryService.Domain.Department.ValueObject;
using Path = DirectoryService.Domain.Department.ValueObject.Path;

namespace DirectoryService.Domain.Department;

public class Department
{
    private Department(Name name, Identifier identifier, Path path)
    {
        Id = Guid.NewGuid();
        Name = name;
        Identifier = identifier;
        Path = path;
    }
    
    public Guid Id { get; private set; }
    
    public Name Name { get; private set; }
    
    public Identifier Identifier { get; private set; }
    
    // ├── IT отдел (ParentId = null - это корень)
    //     │   ├── Backend команда (ParentId = ID of "IT отдел")
    // │   └── Frontend команда (ParentId = ID of "IT отдел")
    public Guid? ParentId { get; private set; }
    
    public Path Path { get; private set; }
}