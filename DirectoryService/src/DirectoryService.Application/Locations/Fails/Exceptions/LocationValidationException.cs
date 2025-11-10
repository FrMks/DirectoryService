using DirectoryService.Application.Exceptions;
using Shared;

namespace DirectoryService.Application.Locations.Fails.Exceptions;

public class LocationValidationException : BadRequestException
{
    public LocationValidationException(Error[] errors)
        : base(errors)
    {
    }
}