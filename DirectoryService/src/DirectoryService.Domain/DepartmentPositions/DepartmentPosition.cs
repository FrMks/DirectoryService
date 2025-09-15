using System.Reflection.Emit;
using CSharpFunctionalExtensions;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.DepartmentPositions.ValueObjects;

namespace DirectoryService.Domain;

public class DepartmentPosition : Entity<DepartmentPositionId>
{
    // EF Core
    private DepartmentPosition(DepartmentPositionId id)
        : base(id) { }
    
    private DepartmentPosition(DepartmentPositionId id, DepartmentId departmentId, Guid positionId)
        : base(id)
    {
        Id = id;
        DepartmentId = departmentId;
        PositionId = positionId;
    }

    #region Properties

    public DepartmentPositionId Id { get; private set; }
    
    public DepartmentId DepartmentId { get; private set; }
    
    public Guid PositionId { get; private set; }

    #endregion

    #region Public methods

    public static Result<DepartmentPosition> Create(DepartmentPositionId id, DepartmentId departmentId, Guid positionId)
    {
        DepartmentPosition departmentPosition = new(id, departmentId, positionId);

        return Result.Success(departmentPosition);
    }
    
    public void SetId(DepartmentPositionId id) => Id = id;
    public void SetDepartmentId(DepartmentId departmentId) => DepartmentId = departmentId;
    public void SetPositionId(Guid positionId) => PositionId = positionId;

    #endregion
}