using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Contracts.Departments;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Departments;

public class UpdateDepartmentLocationsHandler(
    IDepartmentsRepository departmentsRepository,
    IValidator<UpdateDepartmentLocationsRequest> validator,
    ILogger<CreateDepartmentHandler> logger)
    : ICommandHandler<Guid, UpdateDepartmentLocationsCommand>
{
    public async Task<Result<Guid, Errors>> Handle(
        UpdateDepartmentLocationsCommand command,
        CancellationToken cancellationToken)
    {
        
    }
}