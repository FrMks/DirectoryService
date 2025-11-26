using DirectoryService.Application.Validation;
using DirectoryService.Contracts.Departments;
using FluentValidation;
using Shared;

namespace DirectoryService.Application.Departments.Validation;

public class UpdateDepartmentLocationsDtoValidator : AbstractValidator<UpdateDepartmentLocationsRequest>
{
    public UpdateDepartmentLocationsDtoValidator()
    {
        RuleFor(d => d.LocationsIds)
            .NotNull()
            .NotEmpty()
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithError(Error.Validation(
                null,
                "В списке идентификаторов локации не должно быть повторяющихся элементов"));
    }
}