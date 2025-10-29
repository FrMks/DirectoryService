using DirectoryService.Application.Exceptions;

namespace DirectoryService.Application.Locations.Exceptions;

public class LocationValidationException : BadRequestException
{
    public LocationValidationException(string? error)
        : base(error)
    {
    }

    public LocationValidationException(IEnumerable<string> errors)
        : base(errors)
    {
    }
}