using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using Shared;

namespace DirectoryService.Domain.Department.ValueObject;

public partial record Path
{
    private Path(string value)
    {
        Value = value;
    }
    
    public string Value { get; init; }

    public static Result<Path, Error> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation(null, "Department path cannot be null or empty");
        
        string trimmedValue = value.Trim();
        
        if (string.IsNullOrWhiteSpace(trimmedValue))
            return Error.Validation(null, "Department path cannot be null or empty");
        
        string result = trimmedValue.Replace(' ', '-');
        
        if (!LatinLettersDotsHyphensRegex().IsMatch(result))
            return Error.Validation(null, "Department path is invalid.");
        
        Path path = new(result);
        
        return Result.Success<Path, Error>(path);
    }
    
    [GeneratedRegex(@"^[a-zA-Z.-]+$")]
    private static partial Regex LatinLettersDotsHyphensRegex();
}