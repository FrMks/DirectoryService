using CSharpFunctionalExtensions;
using DirectoryService.Domain.Positions.ValueObject;
using Shared;

namespace DirectoryService.Domain.Positions;

public sealed class Position
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

    public Result<Position> Create(PositionId id, Name name, Description description, bool isActive,
        DateTime createdAt, DateTime updateAt,
        IReadOnlyList<DepartmentPosition> departmentPositions)
    {
        Position position = new(id, name, description, isActive, createdAt, updateAt,
            departmentPositions);
        
        return Result.Success(position);
    }
}