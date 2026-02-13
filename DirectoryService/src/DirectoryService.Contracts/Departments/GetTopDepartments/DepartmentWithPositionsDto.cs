namespace DirectoryService.Contracts.Departments.GetTopDepartments;

public record DepartmentWithPositionsDto(DepartmentDto Department, int PositionsCount);