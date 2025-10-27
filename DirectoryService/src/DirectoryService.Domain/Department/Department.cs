using CSharpFunctionalExtensions;
using DirectoryService.Domain.Department.ValueObject;
using Shared;
using Path = DirectoryService.Domain.Department.ValueObject.Path;

namespace DirectoryService.Domain.Department;

public sealed class Department
{
    // EF Core
    private Department() { }
    
    private Department(
        DepartmentId id,
        Name name,
        Identifier identifier,
        Path path,
        IEnumerable<DepartmentLocation> departmentLocations,
        IEnumerable<DepartmentPosition> departmentPositions,
        Depth depth, Guid? parentId)
    {
        Id = id;
        Name = name;
        Identifier = identifier;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Path = path;
        DepartmentLocations = departmentLocations.ToList();
        DepartmentPositions = departmentPositions.ToList();
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

    public static Result<Department> Create(
        DepartmentId id,
        Name name,
        Identifier identifier,
        Path path,
        IEnumerable<DepartmentLocation> departmentLocations,
        IEnumerable<DepartmentPosition> departmentPositions,
        Depth depth, Guid? parentId)
    {
        Department department = new(id, name, identifier, path, departmentLocations, departmentPositions, depth, parentId);

        return Result.Success(department);
    }

    public static Result<Department, Error> CreateParent(
        Name name,
        Identifier identifier,
        IEnumerable<DepartmentLocation> departmentLocations,
        IEnumerable<DepartmentPosition> departmentPositions,
        DepartmentId? departmentId = null,
        Guid? parentId = null)
    {
        var path = Path.CreateParent(identifier);

        Result<Depth, Error> depthResult = ValueObject.Depth.Create(0);
        if (depthResult.IsFailure)
        {
            return Error.Validation(null, depthResult.Error.ToString());
        }

        var depth = depthResult.Value;

        Department department = new(
            departmentId ?? DepartmentId.NewDepartmentId(),
            name,
            identifier,
            path,
            departmentLocations,
            departmentPositions,
            depth,
            parentId
            );
        return Result.Success<Department, Error>(department);
    }

    public static Result<Department, Error> CreateChild(
        Name name,
        Identifier identifier,
        Department parent,
        IEnumerable<DepartmentLocation> departmentLocations,
        IEnumerable<DepartmentPosition> departmentPositions,
        DepartmentId? departmentId = null,
        Guid? parentId = null)
    {
        var path = parent.Path.CreateChild(identifier);
        short count = (short)(parent.Depth.Value + 1);
        Result<Depth, Error> depthResult = Depth.Create(count);

        if (depthResult.IsFailure)
        {
            return Error.Validation(null, depthResult.Error.ToString());
        }

        var depth = depthResult.Value;

        Department department = new(
            departmentId ?? DepartmentId.NewDepartmentId(),
            name,
            identifier,
            path,
            departmentLocations,
            departmentPositions,
            depth,
            parentId
        );
        return Result.Success<Department, Error>(department);
    }
}