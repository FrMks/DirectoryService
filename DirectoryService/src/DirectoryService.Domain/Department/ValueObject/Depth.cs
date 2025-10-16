using CSharpFunctionalExtensions;
using Shared;

namespace DirectoryService.Domain.Department.ValueObject;

public record Depth
{
    private Depth(short value)
    {
        Value = value;
    }
    
    public short Value { get; init; }

    public static Result<Depth, Error> Create(short depth)
    {
        if (depth == 0)
            return Error.Failure(null, "Department depth is invalid.");
        
        if (depth <= 0)
            return Error.Failure(null, "Department depth is less or equal to zero.");
        
        if (depth >= 10)
            return Error.Failure(null, "Department depth is greater or equal than 10.");
        
        Depth result = new(depth);
        
        return Result.Success<Depth, Error>(result);
    }
}