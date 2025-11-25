using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Application.Extensions;
using DirectoryService.Application.Positions.Interfaces;
using DirectoryService.Contracts.Positions;
using DirectoryService.Domain.Positions.ValueObject;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Positions;

public class CreatePositionHandler(
    IPositionsRepository positionsRepository,
    IDepartmentsRepository departmentsRepository,
    IValidator<CreatePositionRequest> validator,
    ILogger<CreatePositionHandler> logger)
    : ICommandHandler<Guid, CreatePositionCommand>
{
    public async Task<Result<Guid, Errors>> Handle(CreatePositionCommand command, CancellationToken cancellationToken)
    {
        // Валидация DTO
        var validationResult = await validator.ValidateAsync(command.PositionRequest, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.ToList())
            {
                logger.LogInformation("Error when creating position, error: {error}", error.Message);
            }

            return validationResult.ToList();
        }
        
        // Создание сущности Position
        PositionId positionId = PositionId.NewPositionId();
        
        // TODO: Name не должен совпадать с активной должностью
        var nameResult = Name.Create(command.PositionRequest.Name);
        Name name = nameResult.Value;
        var isNameExistAndNotActive = await positionsRepository.IsNameExistAndNotActive(name, cancellationToken);
        if (isNameExistAndNotActive.IsFailure)
        {
            logger.LogInformation("{isNameExistAndNotActive}", isNameExistAndNotActive.Error.Message);
            return isNameExistAndNotActive.Error.ToErrors();
        }
        
        var descriptionResult = Description.Create(command.PositionRequest.Description);
        Description description = descriptionResult.Value;
        
        var isAllDepartmentsExistAndActive = await departmentsRepository.AllExistAndActiveAsync(command.PositionRequest.DepartmentIds, cancellationToken);
        if (isAllDepartmentsExistAndActive.IsFailure)
        {
            logger.LogInformation("{isAllDepartmentsExistAndActive}", isAllDepartmentsExistAndActive.Error.Message);
            return isAllDepartmentsExistAndActive.Error.ToErrors();
        }
    }
}