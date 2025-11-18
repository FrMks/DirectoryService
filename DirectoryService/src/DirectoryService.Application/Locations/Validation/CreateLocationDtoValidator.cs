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
        
        RuleFor(c => c.Address.Street)
            .Custom((street, context) =>
            {
                var address = context.InstanceToValidate.Address;
                var result = Address.Create(address.Street, address.City, address.Country);
                
                if (!result.IsSuccess)
                    context.AddFailure(result.Error.Message);
            });
            
        RuleFor(c => c.Timezone)
            .MustBeValueObject(Timezone.Create);
    }
}