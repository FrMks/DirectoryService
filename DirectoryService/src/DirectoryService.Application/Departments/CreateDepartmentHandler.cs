using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Application.Extensions;
using DirectoryService.Contracts.Departments;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Departments;

public class CreateDepartmentHandler(
    IDepartmentsRepository departmentsRepository,
    IValidator<CreateDepartmentRequest> validator,
    ILogger<CreateDepartmentHandler> logger)
    : ICommandHandler<Guid, CreateDepartmentCommand>
{
    public async Task<Result<Guid, Errors>> Handle(CreateDepartmentCommand departmentCommand, CancellationToken cancellationToken)
    {
        // Валидация DTO
        var validationResult = await validator.ValidateAsync(departmentCommand.DepartmentRequest, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.ToList())
            {
                logger.LogInformation("Error when creating department, error: {error}", error.Message);
            }

            return validationResult.ToList();
        }
    }
}