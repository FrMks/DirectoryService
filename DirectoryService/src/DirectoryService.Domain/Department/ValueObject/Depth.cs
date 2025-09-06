using CSharpFunctionalExtensions;

namespace DirectoryService.Domain.Department.ValueObject;

public class Depth
{
    private Depth(short value)
    {
        Value = value;
    }
    
    public short Value { get; private set; }

    public static Result<Depth> Create(short depth)
    {
        if (depth == 0)
            Result.Failure<Depth>("Depth is invalid.");
        
        if (depth <= 0)
            Result.Failure<Depth>("Depth is less or equal to zero.");
        
        if (depth >= 10)
            Result.Failure<Depth>("Depth is greater or equal than 10.");
        
        Depth result = new(depth);
        
        return Result.Success(result);
    }
}