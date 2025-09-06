using System.Security;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;

namespace DirectoryService.Domain.Department.ValueObject;

public partial class Identifier
{

    private Identifier(string identifier)
    {
        Value = identifier;
    }
    
    public string Value { get; private set; }

    public static Result<Identifier> Create(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            Result.Failure("Identifier is required.");
        
        string trimmedIdentifier = identifier.Trim();
        
        if (trimmedIdentifier == string.Empty)
            Result.Failure<Department>("Identifier is required.");
        
        if (trimmedIdentifier.Length < 3 || trimmedIdentifier.Length > 150)
            Result.Failure<Department>("Identifier is invalid.");
        
        if (!LatinLettersOnlyRegex().IsMatch(trimmedIdentifier))
            Result.Failure<Department>("Identifier is invalid.");
        
        Identifier instance = new Identifier(identifier);
        
        return Result.Success(instance);
    }

    [GeneratedRegex(@"^[a-zA-Z]+$")]
    private static partial Regex LatinLettersOnlyRegex();

}