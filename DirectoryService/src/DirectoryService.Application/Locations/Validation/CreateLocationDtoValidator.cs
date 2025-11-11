using DirectoryService.Contracts.Locations;
using FluentValidation;

namespace DirectoryService.Application.Locations.Validation;

public class CreateLocationDtoValidator : AbstractValidator<CreateLocationRequest>
{
    public CreateLocationDtoValidator()
    {
        RuleFor(x => x.Name)
        .NotEmpty()
        .MinimumLength(3)
        .MaximumLength(120)
        .WithMessage("Имя находится не в пределах 3-120 символов.");
    }
}