using DirectoryService.Contracts.Departments;
using FluentValidation;

namespace DirectoryService.Application.Departments.Validation;

public class UpdateDepartmentLocationsDtoValidator : AbstractValidator<UpdateDepartmentLocationsRequest>
{
    public UpdateDepartmentLocationsDtoValidator()
    {
        
    }
}