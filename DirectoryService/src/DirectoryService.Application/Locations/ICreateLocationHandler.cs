using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Locations;
using Shared;

namespace DirectoryService.Application.Locations;

public interface ICreateLocationHandler
{
    Task<Result<Guid, Error>> Handle(CreateLocationCommand locationCommand, CancellationToken cancellationToken);
}