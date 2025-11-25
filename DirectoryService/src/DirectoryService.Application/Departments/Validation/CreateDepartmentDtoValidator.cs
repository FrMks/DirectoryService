using DirectoryService.Application.Validation;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Department.ValueObject;
using FluentValidation;
using Shared;

namespace DirectoryService.Application.Departments;

public class CreateDepartmentDtoValidator : AbstractValidator<CreateDepartmentRequest>
{
    public CreateDepartmentDtoValidator()
    {
        RuleFor(d => d.Name)
            .MustBeValueObject(Name.Create);
        
        RuleFor(d => d.Identifier)
            .MustBeValueObject(Identifier.Create);
        
        RuleFor(d => d.ParentId)
            .Must(id => !id.HasValue || id != Guid.Empty)
            .WithError(Error.Validation(
                null,
                "Идентификатор родителя неверный. Либо null либо не empty, если существует."));

        RuleFor(d => d.LocationsIds)
            .Must(ids => ids.Any())
            .WithError(Error.Validation(
                null,
                "Список идентификаторов локации не должен быть пустым."));

        RuleFor(d => d.LocationsIds)
            .NotNull()
            .NotEmpty()
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithError(Error.Validation(
                null,
                "В списке идентификаторов локации не должно быть повторяющихся элементов"));
    }
}