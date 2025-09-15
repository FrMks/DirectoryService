using CSharpFunctionalExtensions;

namespace DirectoryService.Domain.Positions.ValueObject;

public record Description
{
    private Description(string value)
    {
        Value = value;
    }
    
    public string Value { get; init; }

    public static Result<Description> Create(string value)
    {
        if (string.IsNullOrEmpty(value))
            Result.Failure("Value cannot be null or empty.");
        
        string trimmedValue = value.Trim();
        
        if (string.IsNullOrEmpty(trimmedValue) || trimmedValue.Length > 1000)
            Result.Failure("Value cannot be longer than 1000 characters and empty.");
        
        Description description = new(trimmedValue);
        
        return Result.Success(description);
    }
}