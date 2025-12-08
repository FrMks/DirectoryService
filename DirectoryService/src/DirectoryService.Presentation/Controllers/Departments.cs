using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Departments;
using DirectoryService.Contracts.Departments;
using Microsoft.AspNetCore.Mvc;
using Shared.EndpointResults;

namespace DirectoryService.Web.Controllers;

[ApiController]
[Route("api/departments")]
public class Departments : ControllerBase
{
    [HttpPost]
    public async Task<EndpointResult<Guid>> Create(
        [FromServices] ICommandHandler<Guid, CreateDepartmentCommand> handler,
        [FromBody] CreateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        CreateDepartmentCommand departmentCommand = new(request);
        
        var result = await handler.Handle(departmentCommand, cancellationToken);
        
        if (result.IsFailure)
            return result.ConvertFailure<Guid>();
        
        return result;
    }

    [HttpPut("{departmentId}/locations")]
    public async Task<EndpointResult<Guid>> UpdateDepartmentLocations(
        Guid departmentId,
        [FromServices] ICommandHandler<Guid, UpdateDepartmentLocationsCommand> handler,
        [FromBody] UpdateDepartmentLocationsRequest request,
        CancellationToken cancellationToken)
    {
        UpdateDepartmentLocationsCommand departmentLocationsCommand = new(departmentId, request);

        return await handler.Handle(departmentLocationsCommand, cancellationToken);
    }
}