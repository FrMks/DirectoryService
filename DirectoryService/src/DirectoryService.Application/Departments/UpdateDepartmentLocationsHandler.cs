using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Application.Extensions;
using DirectoryService.Application.Locations.Interfaces;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.Locations.ValueObjects;
using DirectoryService.Domain.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Departments;

public class UpdateDepartmentLocationsHandler(
    IDepartmentsRepository departmentsRepository,
    ILocationsRepository locationsRepository,
    IValidator<UpdateDepartmentLocationsRequest> validator,
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

        DepartmentId departmentId = DepartmentId.FromValue(command.DepartmentId);
        var departmentResult = await departmentsRepository.GetByIdAsync(departmentId, cancellationToken);
        if (departmentResult.IsFailure)
        {
            logger.LogInformation("Error when try get by id department, error: {error}", departmentResult.Error);
            return departmentResult.Error;
        }
        
        var department = departmentResult.Value;
        
        var isAllLocationsExist = await locationsRepository.AllExistAsync(
            command.DepartmentLocationsRequest.LocationsIds, cancellationToken);
        if (isAllLocationsExist.IsFailure)
        {
            logger.LogInformation(
                "Try check existing locations id database, error: {isAllLocationsExist.Error}",
                isAllLocationsExist.Error.Message);
            return isAllLocationsExist.Error.ToErrors();
        }

        var departmentLocations =
            command.DepartmentLocationsRequest.LocationsIds.Select(
                id => DepartmentLocation.Create(
                    DepartmentLocationId.NewDepartmentId(),
                    departmentId,
                    LocationId.FromValue(id)).Value);
        
        var updateResult = department.UpdateDepartmentLocations(departmentLocations);
        if (updateResult.IsFailure)
        {
            logger.LogInformation("Error when updating department locations, error: {error}", updateResult.Error);
            return updateResult.Error.ToErrors();
        }
        
        return await departmentsRepository.SaveChanges(cancellationToken);
    }
}