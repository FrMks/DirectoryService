using CSharpFunctionalExtensions;

namespace DirectoryService.Domain.Locations.ValueObjects;

public record Name
{
    // TODO: сделать валидацию
    public Name(string value)
    {
        Value = value;
    }

    public string Value { get; init; }

    public static Result<Name> Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result.Failure<Name>("Name cannot be empty");

        string trimmed = input.Trim();

        if (trimmed.Length < LengthConstants.LENGTH3 || trimmed.Length > LengthConstants.LENGTH120)
            return Result.Failure<Name>("Name cannot be less than 3 symbols and more than 120 characters");

        Name name = new(trimmed);

        return Result.Success(name);
    }
}