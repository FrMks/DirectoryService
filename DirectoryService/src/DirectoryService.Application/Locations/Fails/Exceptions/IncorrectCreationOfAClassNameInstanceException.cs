using DirectoryService.Application.Exceptions;

namespace DirectoryService.Application.Locations.Fails.Exceptions;

public class IncorrectCreationOfAClassNameInstanceException : BadRequestException
{
    public IncorrectCreationOfAClassNameInstanceException()
        : base([Errors.Locations.IncorrectCreationOfAClassNameInstance()])
    {
    }
}