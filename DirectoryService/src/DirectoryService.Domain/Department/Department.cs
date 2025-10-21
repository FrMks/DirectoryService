using CSharpFunctionalExtensions;
using DirectoryService.Domain.Department.ValueObject;
using Path = DirectoryService.Domain.Department.ValueObject.Path;

namespace DirectoryService.Domain.Department;

public sealed class Department
{
    // EF Core
    private Department() { }
    
    private Department(DepartmentId id, Name name, Identifier identifier, Path path,
        bool isActive, DateTime createdAt, DateTime updatedAt,
        IReadOnlyList<DepartmentLocation> departmentLocations,
        IReadOnlyList<DepartmentPosition> departmentPositions,
        Depth depth, Guid? parentId)
    {
        Id = id;
        Name = name;
        Identifier = identifier;
        Path = path;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        DepartmentLocations = departmentLocations;
        DepartmentPositions = departmentPositions;
        Depth = depth;
        ParentId = parentId;
    }

    #region Properties

    public DepartmentId Id { get; private set; }
    
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
    
    public DateTime UpdatedAt { get; private set; }
    
    public IReadOnlyList<DepartmentLocation> DepartmentLocations { get; private set; } = null!;

    public IReadOnlyList<DepartmentPosition> DepartmentPositions { get; private set; } = null!;

    #endregion

    public static Result<Department> Create(DepartmentId id, Name name, Identifier identifier, Path path,
        bool isActive, DateTime createdAt, DateTime updatedAt,
        IReadOnlyList<DepartmentLocation> departmentLocations,
        IReadOnlyList<DepartmentPosition> departmentPositions,
        Depth depth, Guid? parentId)
    {
        Department department = new(id, name, identifier, path, isActive, createdAt, updatedAt, departmentLocations, departmentPositions, depth, parentId);
        
        return Result.Success(department);
    }
}