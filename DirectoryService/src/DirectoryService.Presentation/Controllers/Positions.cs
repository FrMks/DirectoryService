using DirectoryService.Application.Abstractions;
using DirectoryService.Contracts.Positions;
using Microsoft.AspNetCore.Mvc;
using Shared.EndpointResults;

namespace DirectoryService.Web.Controllers;

[ApiController]
[Route("api/positions")]
public class Positions
{
    [HttpPost]
    public async Task<EndpointResult<Guid>> Create(
        [FromServices] ICommandHandler<Guid, CreatePositionCommand> handler,
        [FromBody] CreatePositionRequest request,
        CancellationToken cancellationToken)
    {
        CreatePositionCommand command = new(request);
        
        var result = await handler.Handle(command, cancellationToken);
        
        if (result.IsFailure)
            return result.ConvertFailure<Guid>();
        
        return result;
    }
}