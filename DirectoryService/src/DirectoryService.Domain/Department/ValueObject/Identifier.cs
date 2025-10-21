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
        
        if (trimmedIdentifier.Length is < 3 or > 150)
            return Error.Validation("lenght.is.invalid", "Department identifier is invalid.");
        
        if (!LatinLettersOnlyRegex().IsMatch(trimmedIdentifier))
            return Error.Validation(null, "Department identifier is invalid.");
        
        Identifier instance = new(identifier);
        
        return Result.Success<Identifier, Error>(instance);
    }

    [GeneratedRegex(@"^[a-zA-Z]+$")]
    private static partial Regex LatinLettersOnlyRegex();

}