using CSharpFunctionalExtensions;
using DirectoryService.Domain.Department.ValueObject;
using Path = DirectoryService.Domain.Department.ValueObject.Path;

namespace DirectoryService.Domain.Department;

public class Department
{
    private Department(Name name, Identifier identifier, Path path,
        bool isActive, DateTime createdAt, DateTime updateAt,
        IReadOnlyList<DepartmentLocation> departmentLocations,
        IReadOnlyList<DepartmentPosition> departmentPositions)
    {
        Id = Guid.NewGuid();
        Name = name;
        Identifier = identifier;
        Path = path;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdateAt = updateAt;
        DepartmentLocations = departmentLocations;
        DepartmentPositions = departmentPositions;
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
    
    public bool IsActive { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    
    public DateTime UpdateAt { get; private set; }
    
    public IReadOnlyList<DepartmentLocation> DepartmentLocations { get; private set; }
    
    public IReadOnlyList<DepartmentPosition> DepartmentPositions { get; private set; }

    #endregion

    #region Public methods

    public Result<Department> Create(Name name, Identifier identifier, Path path,
        bool isActive, DateTime createdAt, DateTime updateAt,
        IReadOnlyList<DepartmentLocation> departmentLocations,
        IReadOnlyList<DepartmentPosition> departmentPositions)
    {
        Department department = new(name, identifier, path, isActive, createdAt, updateAt, departmentLocations, departmentPositions);
        
        return Result.Success(department);
    }
    
    public void SetName(Name name) => Name = name;
    public void SetIdentifier(Identifier identifier) => Identifier = identifier;
    public void SetPath(Path path) => Path = path;
    public void SetDepth(Depth depth) => Depth = depth;
    public void SetIsActive(bool isActive) => IsActive = isActive;
    public void SetCreatedAt(DateTime createdAt) => CreatedAt = createdAt;
    public void SetUpdateAt(DateTime updateAt) => UpdateAt = updateAt;
    public void SetDepartmentLocations(IReadOnlyList<DepartmentLocation> departmentLocations) => DepartmentLocations = departmentLocations;

    #endregion
}