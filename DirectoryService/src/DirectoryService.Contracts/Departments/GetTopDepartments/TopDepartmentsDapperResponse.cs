namespace DirectoryService.Contracts.Departments.GetTopDepartments;

public record TopDepartmentsDapperResponse(List<DepartmentWithPositionsDto> Departments);