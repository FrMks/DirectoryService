using CSharpFunctionalExtensions;
using Shared;

namespace DirectoryService.Domain.Department.ValueObject;

public record Name
{
    private Name(string value)
    {
        Value = value;
    }

    public string Value { get; init; }

    public static Result<Name, Error> Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Error.Validation(null, "Department name cannot be empty");

        string trimmed = input.Trim();

        if (trimmed.Length is < LengthConstants.LENGTH3 or > LengthConstants.LENGTH150)
            return Error.Validation("lenght.is.invalid", "Department name cannot be less than 3 symbols and more than 150 characters");

        Name name = new(trimmed);

        return Result.Success<Name, Error>(name);
    }
}