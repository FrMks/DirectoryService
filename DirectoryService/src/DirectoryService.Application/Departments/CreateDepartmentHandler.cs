using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Application.Extensions;
using DirectoryService.Application.Locations;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain;
using DirectoryService.Domain.Department;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.Locations.ValueObjects;
using DirectoryService.Domain.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared;
using Name = DirectoryService.Domain.Department.ValueObject.Name;
using Path = DirectoryService.Domain.Department.ValueObject.Path;

namespace DirectoryService.Application.Departments;

public class CreateDepartmentHandler(
    IDepartmentsRepository departmentsRepository,
    ILocationsRepository locationsRepository,
    IValidator<CreateDepartmentRequest> validator,
    ILogger<CreateDepartmentHandler> logger)
    : ICommandHandler<Guid, CreateDepartmentCommand>
{
    public async Task<Result<Guid, Errors>> Handle(
        CreateDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        // Валидация DTO
        var validationResult = await validator.ValidateAsync(command.DepartmentRequest, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.ToList())
            {
                logger.LogInformation("Error when creating department, error: {error}", error.Message);
            }

            return validationResult.ToList();
        }
        
        // Создание сущности Department
        DepartmentId departmentId = DepartmentId.NewDepartmentId();
        
        var nameResult = Name.Create(command.DepartmentRequest.Name);
        Name departmentName = nameResult.Value;
        
        var identifierResult = Identifier.Create(command.DepartmentRequest.Identifier);
        // Проверяем, что identifier уникальный.
        var isUnique = await departmentsRepository
            .IsIdentifierIsUniqueAsync(identifierResult.Value, cancellationToken);
        if (isUnique.IsFailure)
        {
            logger.LogInformation("{isIdentifierIsUniqueAsync.Error.Message}", isUnique.Error.Message);
            return isUnique.Error.ToErrors();
        }

        var identifier = identifierResult.Value;

        var parentIdResult = command.DepartmentRequest.ParentId;

        Path path;
        Depth depth;
        Department? parent = null;
        // ParentId пустой, следовательно, будем создавать родительский Department.
        if (parentIdResult == null)
        {
            // Создаем родительский путь
            path = Path.CreateParent(identifier);

            // Устанавливаем Depth в 0, так как является корнем.
            var depthResult = Depth.Create(0);
            if (depthResult.IsFailure)
            {
                logger.LogInformation("{parentDepartmentResult.Error}", depthResult.Error);
                return depthResult.Error.ToErrors();
            }

            depth = depthResult.Value;
        }
        else // Создаем дочерний Department.
        {
            var parentResult = await departmentsRepository
                .GetByIdAsync(DepartmentId.FromValue(parentIdResult.Value), cancellationToken);

            if (parentResult.IsFailure)
            {
                logger.LogInformation("{parentDepartmentResult.Error}", parentResult.Error);
                return parentResult.Error;
            }
            
            parent = parentResult.Value;

            path = parent.Path.CreateChild(identifier);
            short depthCount = (short)(parent.Depth.Value + 1);
            var depthResult = Depth.Create(depthCount);

            if (depthResult.IsFailure)
            {
                logger.LogInformation(depthResult.Error.Message);
                return depthResult.Error.ToErrors();
            }
            
            depth = depthResult.Value;
        }

        var isAllLocationsExist = await locationsRepository.AllExistAsync(
            command.DepartmentRequest.LocationsIds,
            cancellationToken);
        if (isAllLocationsExist.IsFailure)
        {
            logger.LogInformation("{isAllLocationsExist.Error}", isAllLocationsExist.Error.Message);
            return isAllLocationsExist.Error.ToErrors();
        }
        
        // Создание department location
        var departmentLocations =
            command.DepartmentRequest.LocationsIds.Select(li => DepartmentLocation.Create(
                DepartmentLocationId.FromValue(Guid.NewGuid()),
                departmentId,
                LocationId.FromValue(li)).Value);
        
        // TODO: как мне правильно проинициализировать свойство departmentPositions?
        var department = Department.Create(
            departmentId,
            departmentName,
            identifier,
            path,
            departmentLocations,
            new List<DepartmentPosition>(),
            depth,
            parentIdResult.Value).Value;
        
        logger.LogInformation("Creating department with id {id}", department.Id.Value);
        
        // Сохранение сущность Department в БД
        var successfulId = await departmentsRepository.AddAsync(department, cancellationToken);
        
        if (successfulId.IsFailure)
            return Error.Failure(null, successfulId.Error.Message).ToErrors();
        
        // Логирование об успешном или неуспешном сохранении
        logger.LogInformation("Department with id {successfulId.Value} add to db.", successfulId.Value);
        
        return successfulId.Value;
    }
}