using CSharpFunctionalExtensions;
using Shared;

namespace DirectoryService.Domain.Locations.ValueObjects;

public record Name
{
    private Name(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Name, Error> Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Error.Validation(null, "Location name cannot be empty");

        string trimmed = input.Trim();

        if (trimmed.Length is < LengthConstants.LENGTH3 or > LengthConstants.LENGTH120)
            return Error.Validation("lenght.is.invalid", "Location name cannot be less than 3 symbols and more than 120 characters");

        Name name = new(trimmed);

        return Result.Success<Name, Error>(name);
    }
}