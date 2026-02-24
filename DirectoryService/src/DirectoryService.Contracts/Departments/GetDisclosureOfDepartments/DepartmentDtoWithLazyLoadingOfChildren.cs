using DirectoryService.Contracts.Departments.GetTopDepartments;

namespace DirectoryService.Contracts.Departments.GetDisclosureOfDepartments;

public record DepartmentDtoWithLazyLoadingOfChildren(DepartmentDto Department, bool HasMoreChildren);