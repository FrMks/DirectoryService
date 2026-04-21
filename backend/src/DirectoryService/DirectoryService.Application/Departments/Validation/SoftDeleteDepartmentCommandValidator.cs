using DirectoryService.Application.Departments.SoftDeleteDepartment;
using DirectoryService.Application.Validation;
using FluentValidation;
using Shared;

namespace DirectoryService.Application.Departments.Validation;

public class SoftDeleteDepartmentCommandValidator : AbstractValidator<SoftDeleteDepartmentCommand>
{
    public SoftDeleteDepartmentCommandValidator()
    {
        RuleFor(x => x.DepartmentId)
            .Must(id => id != Guid.Empty)
            .WithError(Error.Validation(
                "department.id.invalid",
                "DepartmentId must not be empty."));
    }
}
