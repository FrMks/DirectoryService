using Shared.Exceptions;

namespace DirectoryService.Application.Locations.Fails.Exceptions;

public class IncorrectCreationOfAClassAddressInstanceException : BadRequestException
{
    public IncorrectCreationOfAClassAddressInstanceException()
        : base([Errors.Locations.IncorrectCreationOfAClassAddressInstance()])
    {
    }
}