using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Departments.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Departments;

public class CreateDepartmentHandler(
    IDepartmentsRepository departmentsRepository,
    IValidator<CreateDepartmentDtoValidator> validator,
    ILogger<CreateDepartmentHandler> logger)
    : ICommandHandler<Guid, CreateDepartmentCommand>
{
    public Task<Result<Guid, Errors>> Handle(CreateDepartmentCommand command, CancellationToken cancellationToken)
    {
        
    }
}