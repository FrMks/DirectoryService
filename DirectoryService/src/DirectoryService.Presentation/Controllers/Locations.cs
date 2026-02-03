using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Locations;
using DirectoryService.Contracts.Locations;
using DirectoryService.Contracts.Locations.GetLocations;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.EndpointResults;

namespace DirectoryService.Web.Controllers;

[ApiController]
[Route("api/locations")]
public class Locations : ControllerBase
{
    [HttpPost]
    public async Task<EndpointResult<Guid>> Create(
        [FromServices] ICommandHandler<Guid, CreateLocationCommand> handler,
        [FromBody] CreateLocationRequest request,
        CancellationToken cancellationToken)
    {
        CreateLocationCommand locationCommand = new(request);

        var result = await handler.Handle(locationCommand, cancellationToken);

        if (result.IsFailure)
            return result.ConvertFailure<Guid>();

        return result;
    }

    [HttpGet]
    public async Task<EndpointResult<(List<GetLocationsResponse> Locations, long TotalCount)>> Get(
        [FromServices] IQueryHandler<GetLocationsQuery, Result<(List<GetLocationsResponse>, long TotalCount), Errors>> handler,
        [FromQuery] GetLocationsRequest request,
        CancellationToken cancellationToken)
    {
        GetLocationsQuery locationsQuery = new(request);
        var result = await handler.Handle(locationsQuery, cancellationToken);

        if (result.IsFailure)
            return result.ConvertFailure<(List<GetLocationsResponse>, long)>();

        return result;
    }
}