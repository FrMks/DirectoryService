using DirectoryService.Application.Exceptions;
using Shared;

namespace DirectoryService.Application.Locations.Exceptions;

public class LocationValidationException : BadRequestException
{
    public LocationValidationException(Error[] errors)
        : base(errors)
    {
    }
}