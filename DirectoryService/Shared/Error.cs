namespace Shared;

public class Error
{
    private const string SEPARATOR = "||";
    
    public string Code { get; set; }

    public string Message { get; set; }

    public ErrorType Type { get; set; }

    public string? InvalidField { get; set; }

    // У меня не работала программа без этого конструктора, если выбрасывать exception
    // public Error()
    // {
    // }

    private Error(string code, string message, ErrorType type, string? invalidField = null)
    {
        Code = code;
        Message = message;
        Type = type;
        InvalidField = invalidField;
    }

    /// <summary>
    /// When not found in database.
    /// </summary>
    public static Error NotFound(string? code, string message, Guid? id) => new(code ?? "record.not.found", message, ErrorType.NOT_FOUND);

    public static Error Validation(string? code, string message, string? invalidField = null) => new(code ?? "value.is.invalid", message, ErrorType.VALIDATION, invalidField);

    public static Error Conflict(string? code, string message) => new(code ?? "value.is.conflict", message, ErrorType.CONFLICT);

    public static Error Failure(string? code, string message) => new(code ?? "failure", message, ErrorType.FAILURE);

    public string Serialize()
    {
        return string.Join(SEPARATOR, Code, Message, Type);
    }
    
    public static Error Deserialize(string serialized)
    {
        string[] parts = serialized.Split(SEPARATOR);

        if (parts.Length < 3)
        {
            throw new ArgumentException("Invalid serialized format");
        }

        if (Enum.TryParse<ErrorType>(parts[2], out var type) == false)
        {
            throw new ArgumentException("Invalid serialized format");
        }
        
        return new Error(parts[0], parts[1], type);
    }
    
    // Хотим из Error сделать Errors
    public Errors ToErrors() => this;
}