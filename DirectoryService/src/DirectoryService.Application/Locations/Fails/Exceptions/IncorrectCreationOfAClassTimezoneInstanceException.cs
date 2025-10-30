using DirectoryService.Application.Exceptions;

namespace DirectoryService.Application.Locations.Fails.Exceptions;

public class IncorrectCreationOfAClassTimezoneInstanceException : BadRequestException
{
    public IncorrectCreationOfAClassTimezoneInstanceException()
        : base([Errors.Locations.IncorrectCreationOfAClassTimezoneInstance()])
    {
    }
}