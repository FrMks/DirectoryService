using System.Reflection.Emit;
using CSharpFunctionalExtensions;
using DirectoryService.Domain.Locations;

namespace DirectoryService.Domain;

public class DepartmentPosition
{
    // EF Core
    private DepartmentPosition() { }
    
    private DepartmentPosition(Guid id, Guid departmentId, Guid positionId)
    {
        Id = id;
        DepartmentId = departmentId;
        PositionId = positionId;
    }

    #region Properties

    public Guid Id { get; private set; }
    
    public Guid DepartmentId { get; private set; }
    
    public Guid PositionId { get; private set; }

    #endregion

    #region Public methods

    public static Result<DepartmentPosition> Create(Guid id, Guid departmentId, Guid positionId)
    {
        DepartmentPosition departmentPosition = new(id, departmentId, positionId);

        return Result.Success(departmentPosition);
    }
    
    public void SetId(Guid id) => Id = id;
    public void SetDepartmentId(Guid departmentId) => DepartmentId = departmentId;
    public void SetPositionId(Guid positionId) => PositionId = positionId;

    #endregion
}