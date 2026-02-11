using DirectoryService.Domain.Department;

namespace DirectoryService.Contracts.Departments.GetTopDepartments;

public record DepartmentWithPositionsDto(Department Department, int PositionsCount);