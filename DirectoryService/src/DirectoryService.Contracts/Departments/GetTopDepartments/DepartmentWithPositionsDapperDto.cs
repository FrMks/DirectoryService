namespace DirectoryService.Contracts.Departments.GetTopDepartments;

public record DepartmentWithPositionsDapperDto(DepartmentDto Department, int PositionsCount);