using DirectoryService.Application.Exceptions;

namespace DirectoryService.Application.Locations.Exceptions;

public class LocationNotFoundException : NotFoundException
{
    public LocationNotFoundException(Guid id)
        : base("Location", id)
    {
    }
}