using DirectoryService.Contracts.Departments;
using FluentValidation;

namespace DirectoryService.Application.Departments.Validation;

public class UpdateParentLevelDtoValidator : AbstractValidator<UpdateParentLevelRequest>
{
    public UpdateParentLevelDtoValidator()
    {
        // Либо uuid или null (новый родительский отдел), родитель должен существовать и быть активным. 
        RuleFor(d => d.ParentDepartmentId)
            .NotEmpty();
    }
}