using CSharpFunctionalExtensions;
using DirectoryService.Application.Locations;
using DirectoryService.Contracts.Locations;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryService.Web.Controllers;

[ApiController]
[Route("api/locations")]
public class Locations : ControllerBase
{
    [HttpPost]
    public async Task<Result<Guid>> Create(
        [FromServices] ICreateLocationHandler handler,
        [FromBody] CreateLocationRequest request,
        CancellationToken cancellationToken)
    {
        return await handler.Handle(request, cancellationToken);
    }
}