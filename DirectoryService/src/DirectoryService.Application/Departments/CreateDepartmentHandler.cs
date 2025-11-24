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
        
        // Создание сущности Department
        DepartmentId departmentId = DepartmentId.NewDepartmentId();
        
        var departmentNameResult = Name.Create(departmentCommand.DepartmentRequest.Name);
        Name departmentName = departmentNameResult.Value;
        
        var departmentIdentifierResult = Identifier.Create(departmentCommand.DepartmentRequest.Identifier);
        
        // Проверяем, что identifier уникальный.
        var isIdentifierIsUniqueAsync = await departmentsRepository
            .IsIdentifierIsUniqueAsync(departmentIdentifierResult.Value, cancellationToken);
        if (isIdentifierIsUniqueAsync.IsFailure)
        {
            return Error.Failure(
                    isIdentifierIsUniqueAsync.Error.Code,
                    isIdentifierIsUniqueAsync.Error.Message).ToErrors();
        }
        
        var departmentIdentifier = departmentIdentifierResult.Value;

        var departmentParentIdResult = departmentCommand.DepartmentRequest.ParentId;

        Path departmentPath;
        Depth departmentDepth;
        Department? parentDepartment = null;
        // ParentId пустой, следовательно, будем создавать родительский Department.
        if (departmentParentIdResult == null)
        {
            // Создаем родительский путь
            departmentPath = Path.CreateParent(departmentIdentifier);

            // Устанавливаем Depth в 0, так как является корнем.
            var departmentDepthResult = Depth.Create(0);
            if (departmentDepthResult.IsFailure)
            {
                logger.LogInformation("{parentDepartmentResult.Error}", departmentDepthResult.Error);
                return departmentDepthResult.Error.ToErrors();
            }

            departmentDepth = departmentDepthResult.Value;
        }
        // Создаем дочерний Department.
        else
        {
            var parentDepartmentResult = await departmentsRepository
                .GetByIdAsync(DepartmentId.FromValue(departmentParentIdResult.Value), cancellationToken);

            if (parentDepartmentResult.IsFailure)
            {
                logger.LogInformation("{parentDepartmentResult.Error}", parentDepartmentResult.Error);
                return parentDepartmentResult.Error;
            }
            
            parentDepartment = parentDepartmentResult.Value;

            departmentPath = parentDepartment.Path.CreateChild(departmentIdentifier);
            short depthCount = (short)(parentDepartment.Depth.Value + 1);
            var depthResult = Depth.Create(depthCount);

            if (depthResult.IsFailure)
            {
                logger.LogInformation(depthResult.Error.Message);
                return depthResult.Error.ToErrors();
            }
            
            departmentDepth = depthResult.Value;
        }

        var isAllLocationsExist = await locationsRepository.AllExistAsync(
            departmentCommand.DepartmentRequest.LocationsIds,
            cancellationToken);
        if (isAllLocationsExist.IsFailure)
        {
            logger.LogInformation("{isAllLocationsExist.Error}", isAllLocationsExist.Error.Message);
            return isAllLocationsExist.Error.ToErrors();
        }
        
        // Создание department location
        var departmentLocations =
            departmentCommand.DepartmentRequest.LocationsIds.Select(li => DepartmentLocation.Create(
                DepartmentLocationId.FromValue(Guid.NewGuid()),
                departmentId,
                LocationId.FromValue(li)).Value);
        
        // TODO: как мне правильно проинициализировать свойство departmentPositions?
        var department = Department.Create(
            departmentId,
            departmentName,
            departmentIdentifier,
            departmentPath,
            departmentLocations,
            new List<DepartmentPosition>(),
            departmentDepth,
            departmentParentIdResult.Value).Value;
        
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