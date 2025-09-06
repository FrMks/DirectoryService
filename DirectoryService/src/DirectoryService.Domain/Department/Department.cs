using DirectoryService.Domain.Department.ValueObject;
using Path = DirectoryService.Domain.Department.ValueObject.Path;

namespace DirectoryService.Domain.Department;

public class Department
{
    private Department(Name name, Identifier identifier, Path path,
        IReadOnlyList<DepartmentLocation> departmentLocations)
    {
        Id = Guid.NewGuid();
        Name = name;
        Identifier = identifier;
        Path = path;
        
        DepartmentLocations = departmentLocations;
    }

    #region Properties

    public Guid Id { get; private set; }
    
    public Name Name { get; private set; }
    
    public Identifier Identifier { get; private set; }
    
    // ├── IT отдел (ParentId = null - это корень)
    //     │   ├── Backend команда (ParentId = ID of "IT отдел")
    // │   └── Frontend команда (ParentId = ID of "IT отдел")
    public Guid? ParentId { get; private set; }
    
    public Path Path { get; private set; }
    
    public Depth Depth { get; private set; }
    
    public IReadOnlyList<DepartmentLocation> DepartmentLocations { get; private set; }

    #endregion

    #region Public methods

    public void SetName(Name name) => Name = name;
    public void SetIdentifier(Identifier identifier) => Identifier = identifier;
    public void SetPath(Path path) => Path = path;
    public void SetDepth(Depth depth) => Depth = depth;
    
    
    public void SetDepartmentLocations(IReadOnlyList<DepartmentLocation> departmentLocations) => DepartmentLocations = departmentLocations;

    #endregion
}