using DirectoryService.Application.Validation;
using DirectoryService.Contracts.Positions;
using DirectoryService.Domain.Positions.ValueObject;
using FluentValidation;
using Shared;

namespace DirectoryService.Application.Positions.Validation;

public class CreatePositionDtoValidator : AbstractValidator<CreatePositionRequest>
{
    public CreatePositionDtoValidator()
    {
        RuleFor(p => p.Name)
            .MustBeValueObject(Name.Create);
        
        RuleFor(p => p.Description)
            .MustBeValueObject(Description.Create);

        RuleFor(p => p.DepartmentIds)
            .NotNull()
            .NotEmpty()
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithError(Error.Validation(
                null,
                "В списке идентификаторов позиции не должно быть повторяющихся элементов" +
                " и список не должен быть пустой"));
    }
}