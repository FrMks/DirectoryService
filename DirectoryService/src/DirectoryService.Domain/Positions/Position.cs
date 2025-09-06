using CSharpFunctionalExtensions;
using DirectoryService.Domain.Positions.ValueObject;

namespace DirectoryService.Domain.Positions;

public class Position
{
    private Position(Guid id, Name name, Description description,
        IReadOnlyList<DepartmentPosition> departmentPositions)
    {
        Id = id;
        Name = name;
        Description = description;
        
        DepartmentPositions = departmentPositions;
    }

    #region Properties

    public Guid Id { get; private set; }
    public Name Name { get; private set; }
    public Description Description { get; private set; }
    
    public IReadOnlyList<DepartmentPosition> DepartmentPositions { get; private set; }

    #endregion

    #region Public methods

    public Result<Position> Create(Guid id, Name name, Description description,
        IReadOnlyList<DepartmentPosition> departmentPositions)
    {
        Position position = new(id, name, description,
            departmentPositions);
        
        return Result.Success(position);
    }
    
    public void SetId(Guid id) => Id = id;
    public void SetName(Name name) => Name = name;
    public void SetDescription(Description description) => Description = description;
    
    public void SetDepartmentPositions(IReadOnlyList<DepartmentPosition> departmentPositions) => DepartmentPositions = departmentPositions;

    #endregion
}