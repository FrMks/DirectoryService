using CSharpFunctionalExtensions;
using Shared;

namespace DirectoryService.Domain.Department.ValueObject;

public record Depth
{
    private Depth(short value)
    {
        Value = value;
    }
    
    public short Value { get; }

    public static Result<Depth, Error> Create(short depth)
    {
        if (depth < 0 || depth >= 10)
        {
            return Error.Failure(
                "lenght.is.invalid",
                "Department depth must be greater than 0 and less than 10.");
        }

        Depth result = new(depth);
        
        return Result.Success<Depth, Error>(result);
    }
}