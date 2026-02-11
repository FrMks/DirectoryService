namespace DirectoryService.Contracts.Departments.GetTopDepartments;

public record TopDepartmentsResponse(List<DepartmentWithPositionsDto> Departments);
