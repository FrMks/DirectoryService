using DirectoryService.Application.Validation;
using DirectoryService.Contracts.Locations;
using DirectoryService.Domain.Locations.ValueObjects;
using FluentValidation;
using Address = DirectoryService.Domain.Locations.ValueObjects.Address;

namespace DirectoryService.Application.Locations.Validation;

public class CreateLocationDtoValidator : AbstractValidator<CreateLocationRequest>
{
    public CreateLocationDtoValidator()
    {
        RuleFor(c => c.Name)
            .MustBeValueObject(Name.Create);

        RuleFor(c => c.Address)
            .MustBeValueObject(a =>
                Address.Create(
                    a.Street,
                    a.City,
                    a.Country));
            
        RuleFor(c => c.Timezone)
            .MustBeValueObject(Timezone.Create);
    }
}