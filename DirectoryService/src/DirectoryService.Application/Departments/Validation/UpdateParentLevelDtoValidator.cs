using DirectoryService.Contracts.Departments;
using FluentValidation;

namespace DirectoryService.Application.Departments.Validation;

public class UpdateParentLevelDtoValidator : AbstractValidator<UpdateParentLevelRequest>
{
    public UpdateParentLevelDtoValidator()
    {
        // Либо uuid или null (новый родительский отдел), родитель должен существовать и быть активным. 
        RuleFor(d => d.ParentDepartmentId)
            .Must(id => id == null || id != Guid.Empty)
            .WithMessage("ParentDepartmentId must be null or a valid Guid");
    }
}