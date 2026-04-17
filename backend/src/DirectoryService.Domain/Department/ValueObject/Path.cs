using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using Shared;

namespace DirectoryService.Domain.Department.ValueObject;

public partial record Path
{
    private const char SEPARATOR = '.';
    
    public string Value { get; }
    
    private Path(string value)
    {
        Value = value;
    }

    public static Path CreateParent(Identifier identifier)
    {
        string trimmedIdentifier = identifier.Value.Trim();
        string replacesTrimmedIdentifier = trimmedIdentifier.Replace(' ', '-');
        return new Path(replacesTrimmedIdentifier);
    }

    public Path CreateChild(Identifier childIdentifier)
    {
        string identifier = childIdentifier.Value;
        string trimmedIdentifier = identifier.Trim();
        string replacedTrimmedIdentifier = trimmedIdentifier.Replace(' ', '-');
        return new Path(Value + SEPARATOR + replacedTrimmedIdentifier);
    }
    
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

    public static Result<Path, Error> CreateDeletedBranchPath(string currentPath)
    {
        var segments = currentPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return Error.Validation("department.path.invalid", "Department path is invalid.");

        segments[^1] = $"deleted-{segments[^1]}";
        var updatedPath = string.Join('.', segments);

        return Create(updatedPath);
    }

    [GeneratedRegex(@"^[a-zA-Z.-]+$")]
    private static partial Regex LatinLettersDotsHyphensRegex();
}