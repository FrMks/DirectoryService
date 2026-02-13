using DirectoryService.Contracts.Departments.GetTopDepartments;

namespace DirectoryService.Contracts.Departments.GetDisclosureOfDepartments;

public class DepartmentDtoWithLazyLoadingOfChildren(DepartmentDto Department, DepartmentDto[] Children);