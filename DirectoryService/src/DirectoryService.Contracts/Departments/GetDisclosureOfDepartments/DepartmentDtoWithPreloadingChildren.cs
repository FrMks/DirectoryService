using DirectoryService.Contracts.Departments.GetTopDepartments;

namespace DirectoryService.Contracts.Departments.GetDisclosureOfDepartments;

public record DepartmentDtoWithPreloadingChildren(
    DepartmentDto Department,
    bool HasMoreChildren);