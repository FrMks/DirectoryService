using CSharpFunctionalExtensions;
using Shared.Core.Abstractions;
using DirectoryService.Application.Locations;
using DirectoryService.Contracts.Locations;
using DirectoryService.Contracts.Locations.GetLocations;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.Framework.EndpointResults;

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
    public async Task<EndpointResult<GetLocationsResult>> Get(
        [FromServices] IQueryHandler<GetLocationsQuery, Result<GetLocationsResult, Errors>> handler,
        [FromQuery] GetLocationsRequest request,
        CancellationToken cancellationToken)
    {
        GetLocationsQuery locationsQuery = new(request);
        var result = await handler.Handle(locationsQuery, cancellationToken);

        if (result.IsFailure)
            return result.ConvertFailure<GetLocationsResult>();

        return result;
    }

    [HttpPut("{locationId:guid}/preview-asset")]
    public async Task<EndpointResult<Guid>> AttachPreview(
        [FromServices] ICommandHandler<Guid, SetLocationPreviewCommand> handler,
        [FromRoute] Guid locationId,
        [FromBody] AttachLocationPreviewRequest request,
        CancellationToken cancellationToken)
    {
        SetLocationPreviewCommand command = new(locationId, request);

        Result<Guid, Errors> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
            return result.ConvertFailure<Guid>();

        return result;
    }
}
