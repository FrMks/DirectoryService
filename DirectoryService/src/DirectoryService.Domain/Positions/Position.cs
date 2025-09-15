using CSharpFunctionalExtensions;
using DirectoryService.Domain.Positions.ValueObject;

namespace DirectoryService.Domain.Positions;

public class Position
{
    // EF Core
    private Position() { }
    
    private Position(PositionId id, Name name, Description description, bool isActive,
        DateTime createdAt, DateTime updateAt,
        IReadOnlyList<DepartmentPosition> departmentPositions)
    {
        Id = id;
        Name = name;
        Description = description;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdateAt = updateAt;
        DepartmentPositions = departmentPositions;
    }

    #region Properties

    public PositionId Id { get; private set; }
    public Name Name { get; private set; } = null!;
    public Description Description { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdateAt { get; private set; }
    
    public IReadOnlyList<DepartmentPosition> DepartmentPositions { get; private set; } = null!;

    #endregion

    #region Public methods

    public Result<Position> Create(PositionId id, Name name, Description description, bool isActive,
        DateTime createdAt, DateTime updateAt,
        IReadOnlyList<DepartmentPosition> departmentPositions)
    {
        Position position = new(id, name, description, isActive, createdAt, updateAt,
            departmentPositions);
        
        return Result.Success(position);
    }
    
    public void SetId(PositionId id) => Id = id;
    public void SetName(Name name) => Name = name;
    public void SetDescription(Description description) => Description = description;
    public void SetIsActive(bool isActive) => IsActive = isActive;
    public void SetCreatedAt(DateTime createdAt) => CreatedAt = createdAt;
    public void SetUpdateAt(DateTime updateAt) => UpdateAt = updateAt;
    public void SetDepartmentPositions(IReadOnlyList<DepartmentPosition> departmentPositions) => DepartmentPositions = departmentPositions;

    #endregion
}