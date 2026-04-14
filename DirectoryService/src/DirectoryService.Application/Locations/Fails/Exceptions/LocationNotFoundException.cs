using Shared.Exceptions;
using Shared;

namespace DirectoryService.Application.Locations.Exceptions;

public class LocationNotFoundException : NotFoundException
{
    public LocationNotFoundException(Error[] errors)
        : base(errors)
    {
    }
}