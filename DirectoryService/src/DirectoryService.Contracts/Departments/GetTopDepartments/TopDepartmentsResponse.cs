using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DirectoryService.Domain.Department;

namespace DirectoryService.Contracts.Departments.GetTopDepartments;

public record TopDepartmentsResponse(List<DepartmentWithPositionsDto> Departments);