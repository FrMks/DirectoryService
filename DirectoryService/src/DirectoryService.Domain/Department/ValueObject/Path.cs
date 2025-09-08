using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;

namespace DirectoryService.Domain.Department.ValueObject;

public partial record Path
{
    private Path(string value)
    {
        Value = value;
    }
    
    public string Value { get; init; }

    public static Result<Path> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            Result.Failure("Path cannot be null or empty");
        
        string trimmedValue = value.Trim();
        
        if (string.IsNullOrWhiteSpace(trimmedValue))
            return Result.Failure<Path>("Path cannot be null or empty");
        
        string result = trimmedValue.Replace(' ', '-');
        
        if (!LatinLettersDotsHyphensRegex().IsMatch(result))
            Result.Failure<Path>("Path is invalid.");
        
        Path path = new Path(result);
        
        return Result.Success(path);
    }
    
    [GeneratedRegex(@"^[a-zA-Z.-]+$")]
    private static partial Regex LatinLettersDotsHyphensRegex();
}