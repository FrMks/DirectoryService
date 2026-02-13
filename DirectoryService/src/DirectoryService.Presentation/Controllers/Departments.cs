using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Departments;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Departments.GetDisclosureOfDepartments;
using DirectoryService.Contracts.Departments.GetTopDepartments;
using DirectoryService.Contracts.Locations.GetLocations;
using Microsoft.AspNetCore.Mvc;
using Shared;
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

    // departmentId/parent - какой department мы меняем
    // request.DepartmentId - к какому родителю приклеить department, который мы меняем
    [HttpPut("{departmentId}/parent")]
    public async Task<EndpointResult<Guid>> UpdateParentLevel(
        Guid departmentId,
        [FromServices] ICommandHandler<Guid, UpdateParentLevelCommand> handler,
        [FromBody] UpdateParentLevelRequest request,
        CancellationToken cancellationToken)
    {
        UpdateParentLevelCommand updateParentLevelCommand = new(departmentId, request);
        return await handler.Handle(updateParentLevelCommand, cancellationToken);
    }

    [HttpGet("/top-positions")]
    public async Task<EndpointResult<DepartmentWithPositionsDto[]>> GetTopDepartments(
        [FromServices] IQueryHandler<Result<DepartmentWithPositionsDto[], Errors>> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(cancellationToken);

        if (response.IsFailure)
            return response.ConvertFailure<DepartmentWithPositionsDto[]>();

        return response;
    }

    [HttpGet("/top-positions/dapper")]
    public async Task<EndpointResult<DepartmentWithPositionsDapperDto[]>> GetTopDepartmentsDapper(
        [FromServices] IQueryHandler<Result<DepartmentWithPositionsDapperDto[], Errors>> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(cancellationToken);

        if (response.IsFailure)
            return response.ConvertFailure<DepartmentWithPositionsDapperDto[]>();

        return response;
    }

    [HttpGet("roots")]
    public async Task<EndpointResult<DepartmentDtoWithPreloadingChildren[]>> GetRootSectionsWithPreloadingChildren(
        [FromServices] IQueryHandler<GettingRootSectionsWithPreloadingChildrenRequest,
            Result<DepartmentDtoWithPreloadingChildren[], Errors>> handler,
        [FromQuery] GettingRootSectionsWithPreloadingChildrenRequest filter,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(filter, cancellationToken);

        if (response.IsFailure)
            return response.ConvertFailure<DepartmentDtoWithPreloadingChildren[]>();

        return response;
    }

    [HttpGet("{parentId}/children")]
    public async Task<EndpointResult<DepartmentDtoWithLazyLoadingOfChildren[]>> GetDepartmentsWithLazyLoadingOfChildren(
        [FromServices] IQueryHandler<GetDepartmentWithLazyLoadingOfChildrenRequest, Result<DepartmentDtoWithLazyLoadingOfChildren[], Errors>> handler,
        [FromRoute] Guid parentId,
        [FromQuery] PaginationRequest? pagination,
        CancellationToken cancellationToken = default)
    {
        var request = new GetDepartmentWithLazyLoadingOfChildrenRequest(parentId, pagination);
        var response = await handler.Handle(request, cancellationToken);

        if (response.IsFailure)
            return response.ConvertFailure<DepartmentDtoWithLazyLoadingOfChildren[]>();

        return response;
    }
}