using CSharpFunctionalExtensions;
using Shared;

namespace DirectoryService.Domain.Positions.ValueObject;

public record Description
{
    private Description(string value)
    {
        Value = value;
    }
    
    public string Value { get; }

    public static Result<Description, Error> Create(string value)
    {
        if (string.IsNullOrEmpty(value))
            return Error.Validation(null, "Position description cannot be null or empty.");

        string trimmedValue = value.Trim();

        if (string.IsNullOrEmpty(trimmedValue) || trimmedValue.Length > LengthConstants.LENGTH1000)
            return Error.Validation("lenght.is.invalid", "Position description cannot be longer than 1000 characters and empty.");

        Description description = new(trimmedValue);

        return Result.Success<Description, Error>(description);
    }
}