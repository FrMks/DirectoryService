using System.Security;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using Shared;

namespace DirectoryService.Domain.Department.ValueObject;

public partial record Identifier
{
    private Identifier(string identifier)
    {
        Value = identifier;
    }

    public string Value { get; }

    public static Result<Identifier, Error> Create(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return Error.Validation(null, "Department identifier is required.");

        string trimmedIdentifier = identifier.Trim();

        if (trimmedIdentifier == string.Empty)
            return Error.Validation(null, "Department identifier is required.");

        string normalizedIdentifier = trimmedIdentifier.Replace(' ', '-');

        if (normalizedIdentifier.Length is < LengthConstants.LENGTH3 or > LengthConstants.LENGTH150)
            return Error.Validation("lenght.is.invalid", "Department identifier cannot contain more than 150 characters and less than 3 characters.");

        if (!LatinLettersAndHyphenRegex().IsMatch(normalizedIdentifier))
        {
            return Error.Validation(
                null,
                "Department identifier is invalid should contain only latin characters and hyphens.");   
        }

        Identifier instance = new(identifier);

        return Result.Success<Identifier, Error>(instance);
    }

    [GeneratedRegex(@"^[a-zA-Z\-]+$")]
    private static partial Regex LatinLettersAndHyphenRegex();
}