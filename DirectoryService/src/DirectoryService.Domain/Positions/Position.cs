using CSharpFunctionalExtensions;
using DirectoryService.Domain.Positions.ValueObject;

namespace DirectoryService.Domain.Positions;

public class Position
{
    private Position(Guid id, Name name)
    {
        Id = id;
        Name = name;
    }

    #region Properties

    public Guid Id { get; private set; }
    public Name Name { get; private set; }

    #endregion

    #region Public methods

    public Result<Position> Create(Guid id, Name name)
    {
        Position position = new(id, name);
        
        return Result.Success(position);
    }
    
    public void SetId(Guid id) => Id = id;
    public void SetName(Name name) => Name = name;

    #endregion
}