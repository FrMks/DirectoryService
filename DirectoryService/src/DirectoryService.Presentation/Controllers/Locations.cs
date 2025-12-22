using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Locations;
using DirectoryService.Contracts.Locations;
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
    public async Task<EndpointResult<Guid>> Get(
        [FromServices] ICommandHandler<Guid, GetLocationsCommand> handler,
        [FromBody] GetLocationsRequest request,
        CancellationToken cancellationToken)
    {
        GetLocationsCommand locationsCommand = new(request);
        
        var result = await handler.Handle(locationsCommand, cancellationToken);

        if (result.IsFailure)
            return result.ConvertFailure<Guid>();

        return result;
    }
}