using DirectoryService.Domain.Department.ValueObject;
using Path = DirectoryService.Domain.Department.ValueObject.Path;

namespace DirectoryService.Contracts.Departments;

public record DepartmentIdPathAndParentId(DepartmentId DepartmentId, Path Path, DepartmentId? ParentId);
