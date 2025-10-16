using CSharpFunctionalExtensions;
using Shared;

namespace DirectoryService.Domain.Positions.ValueObject;

// TODO: написано сделать уникальным, но на сколько я понимаю,
// то уникальность проверяется по сравнению с чем-то
// (хотя бы есть массив, в котором лежат другие имена позиций)
public record Name
{
    private Name(string value)
    {
        Value = value;
    }

    public string Value { get; init; }

    public static Result<Name, Error> Create(string value)
    {
        if (string.IsNullOrEmpty(value))
            return Error.Validation(null, "Position name cannot be null or empty");

        string trimmedValue = value.Trim();

        if (trimmedValue.Length is < LengthConstants.LENGTH3 or > LengthConstants.LENGTH100)
            return Error.Validation(null, "Position name cannot be less than 3 characters and more than 100 characters");

        Name name = new(trimmedValue);

        return Result.Success<Name, Error>(name);
    }
}