using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Database;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Application.Extensions;
using DirectoryService.Application.Locations.Interfaces;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.Locations.ValueObjects;
using DirectoryService.Domain.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Departments;

public class UpdateDepartmentLocationsHandler(
    IDepartmentsRepository departmentsRepository,
    ILocationsRepository locationsRepository,
    IValidator<UpdateDepartmentLocationsRequest> validator,
    ITransactionManager transactionManager,
    ILogger<CreateDepartmentHandler> logger)
    : ICommandHandler<Guid, UpdateDepartmentLocationsCommand>
{
    public async Task<Result<Guid, Errors>> Handle(
        UpdateDepartmentLocationsCommand command,
        CancellationToken cancellationToken)
    {
        // Валидация DTO
        var validationResult = await validator.ValidateAsync(command.DepartmentLocationsRequest, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (Error error in validationResult.ToList())
            {
                logger.LogInformation("Error when updating department locations, error: {error}", error.Message);
            }

            return validationResult.ToList();
        }
        
        var transactionScopeResult = await transactionManager.BeginTransactionAsTask(cancellationToken);
        if (transactionScopeResult.IsFailure)
            return transactionScopeResult.Error.ToErrors();

        using var transactionScope = transactionScopeResult.Value;

        // Получаем Department из БД по Id
        DepartmentId departmentId = DepartmentId.FromValue(command.DepartmentId);
        var departmentResult = await departmentsRepository.ExistAndActiveAsync(departmentId, cancellationToken);
        if (departmentResult.IsFailure)
        {
            transactionScope.Rollback();
            logger.LogInformation("Error when try get by id department, error: {error}", departmentResult.Error);
            return departmentResult.Error;
        }
        
        var department = departmentResult.Value;
        
        // Все Location существуют внутри БД
        var isAllLocationsExist = await locationsRepository.AllExistAsync(
            command.DepartmentLocationsRequest.LocationsIds, cancellationToken);
        if (isAllLocationsExist.IsFailure)
        {
            transactionScope.Rollback();
            logger.LogInformation(
                "Try check existing locations id database, error: {isAllLocationsExist.Error}",
                isAllLocationsExist.Error.Message);
            return isAllLocationsExist.Error.ToErrors();
        }

        // Создаем новый departmentLocations
        var departmentLocations =
            command.DepartmentLocationsRequest.LocationsIds.Select(
                id => DepartmentLocation.Create(
                    DepartmentLocationId.NewDepartmentId(),
                    departmentId,
                    LocationId.FromValue(id)).Value);
        
        // Обновляем БД
        var updateResult = department.UpdateDepartmentLocations(departmentLocations);
        if (updateResult.IsFailure)
        {
            logger.LogInformation("Error when updating department locations, error: {error}", updateResult.Error);
            return updateResult.Error.ToErrors();
        }
        
        // await departmentsRepository.SaveChanges(cancellationToken);
        await transactionManager.SaveChangesAsync(cancellationToken);
        var commitedResult = transactionScope.Commit();
        if (commitedResult.IsFailure)
        {
            return commitedResult.Error.ToErrors();
        }

        return Result.Success<Guid, Errors>(departmentId.Value);
    }
}