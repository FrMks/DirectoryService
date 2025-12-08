using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Departments;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Application.Extensions;
using DirectoryService.Contracts.Departments;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared;

public class UpdateParentLevelHandler(
    IDepartmentsRepository departmentsRepository,
    IValidator<UpdateParentLevelRequest> validator,
    ILogger<UpdateParentLevelHandler> logger)
    : ICommandHandler<Guid, UpdateParentLevelCommand>
{
    public async Task<Result<Guid, Errors>> Handle(UpdateParentLevelCommand command, CancellationToken cancellationToken)
    {
        // Валидация DTO
        var validationResult = await validator.ValidateAsync(command.ParentLevelRequest, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (Error error in validationResult.ToList())
            {
                logger.LogInformation("Error when updating parent level, error: {error}", error.Message);
            }

            return validationResult.ToList();
        }

        

        return Result.Success<Guid, Errors>(Guid.NewGuid());
    }
}