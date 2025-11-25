using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Application.Extensions;
using DirectoryService.Application.Positions.Interfaces;
using DirectoryService.Contracts.Positions;
using DirectoryService.Domain;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.DepartmentPositions.ValueObjects;
using DirectoryService.Domain.Positions;
using DirectoryService.Domain.Positions.ValueObject;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared;
using Name = DirectoryService.Domain.Positions.ValueObject.Name;

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

        var departmentPositions =
            command.PositionRequest.DepartmentIds.Select(di => DepartmentPosition.Create(
                DepartmentPositionId.FromValue(Guid.NewGuid()),
                DepartmentId.FromValue(di),
                positionId).Value);

        var position = Position.Create(
            positionId,
            name,
            description,
            departmentPositions).Value;
        logger.LogInformation("Creating position with id {id}", position.Id.Value);
        
        // Сохранение сущности Position в БД
        var successfulId = await positionsRepository.AddAsync(position, cancellationToken);

        if (successfulId.IsFailure)
            return successfulId.Error.ToErrors();
        
        // Логирование об успешном или неуспешном сохранении
        logger.LogInformation("Position with id {successfulId.Value} add to db.", successfulId.Value);
        
        return successfulId.Value;
    }
}