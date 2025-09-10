using CSharpFunctionalExtensions;
using DirectoryService.Domain.Department.ValueObject;
using Path = DirectoryService.Domain.Department.ValueObject.Path;

namespace DirectoryService.Domain.Department;

public class Department
{
    // EF Core
    private Department() { }
    
    private Department(Name name, Identifier identifier, Path path,
        bool isActive, DateTime createdAt, DateTime updateAt,
        IReadOnlyList<DepartmentLocation> departmentLocations,
        IReadOnlyList<DepartmentPosition> departmentPositions,
        Depth depth, Guid? parentId)
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
        Depth = depth;
        ParentId = parentId;
    }

    #region Properties

    public Guid Id { get; private set; }
    
    public Name Name { get; private set; } = null!;

    public Identifier Identifier { get; private set; } = null!;

    // ├── IT отдел (ParentId = null - это корень)
    //     │   ├── Backend команда (ParentId = ID of "IT отдел")
    // │   └── Frontend команда (ParentId = ID of "IT отдел")
    public Guid? ParentId { get; private set; }
    
    public Path Path { get; private set; } = null!;

    public Depth Depth { get; private set; } = null!;

    public bool IsActive { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    
    public DateTime UpdateAt { get; private set; }
    
    public IReadOnlyList<DepartmentLocation> DepartmentLocations { get; private set; } = null!;

    public IReadOnlyList<DepartmentPosition> DepartmentPositions { get; private set; } = null!;

    #endregion

    #region Public methods

    public static Result<Department> Create(Name name, Identifier identifier, Path path,
        bool isActive, DateTime createdAt, DateTime updateAt,
        IReadOnlyList<DepartmentLocation> departmentLocations,
        IReadOnlyList<DepartmentPosition> departmentPositions,
        Depth depth, Guid? parentId)
    {
        Department department = new(name, identifier, path, isActive, createdAt, updateAt, departmentLocations, departmentPositions, depth, parentId);
        
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