using CSharpFunctionalExtensions;
using DirectoryService.Domain.Department.ValueObject;
using Shared;
using Path = DirectoryService.Domain.Department.ValueObject.Path;

namespace DirectoryService.Domain.Department;

public sealed class Department
{
    private readonly List<DepartmentLocation> _departmentLocations = [];
    private readonly List<DepartmentPosition> _departmentPositions = [];
    
    // EF Core
    private Department() { }
    
    private Department(
        DepartmentId id,
        Name name,
        Identifier identifier,
        Path path,
        IEnumerable<DepartmentLocation> departmentLocations,
        Depth depth, Guid? parentId)
    {
        Id = id;
        Name = name;
        Identifier = identifier;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Path = path;
        _departmentLocations = departmentLocations.ToList();
        Depth = depth;
        ParentId = parentId;
    }

    #region Properties

    public DepartmentId Id { get; private set; }
    
    public Name Name { get; private set; } = null!;

    public Identifier Identifier { get; private set; } = null!;

    /// <summary>
    /// Идентификатор родителя. null - корневой отдел, иначе родитель существует и активен.
    /// </summary>
    public Guid? ParentId { get; private set; }
    
    public Path Path { get; private set; } = null!;

    public Depth Depth { get; private set; } = null!;

    public bool IsActive { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    
    public DateTime UpdatedAt { get; private set; }
    
    public IReadOnlyList<DepartmentLocation> DepartmentLocations => _departmentLocations;
    public IReadOnlyList<DepartmentPosition> DepartmentPositions => _departmentPositions;

    #endregion

    public static Result<Department> Create(
        DepartmentId id,
        Name name,
        Identifier identifier,
        Path path,
        IEnumerable<DepartmentLocation> departmentLocations,
        Depth depth, Guid? parentId)
    {
        Department department = new(id, name, identifier, path, departmentLocations, depth, parentId);

        return Result.Success(department);
    }

    // TODO: Вопрос. Должно ли у меня тут полностью одно заменяться на другое. Сейчас получается,
    // что у меня добавляются новые departmentLocations 
    public UnitResult<Error> UpdateDepartmentLocations(IEnumerable<DepartmentLocation> departmentLocations)
    {
        var listOfDepartmentLocations = departmentLocations.ToList();
        
        if (listOfDepartmentLocations.Count == 0)
        {
            return Error.Validation(
                "department.location",
                "Department locations must contain at least one location");
        }
        
        _departmentLocations.Clear();
        _departmentLocations.AddRange(listOfDepartmentLocations);
        UpdatedAt = DateTime.UtcNow;

        return UnitResult.Success<Error>();
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
            depth,
            parentId
        );
        return Result.Success<Department, Error>(department);
    }
}