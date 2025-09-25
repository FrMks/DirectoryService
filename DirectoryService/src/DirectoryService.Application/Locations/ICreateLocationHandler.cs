using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Locations;

namespace DirectoryService.Application.Locations;

public interface ICreateLocationHandler
{
    Task<Result<Guid>> Handle(CreateLocationRequest locationRequest, CancellationToken cancellationToken);
}