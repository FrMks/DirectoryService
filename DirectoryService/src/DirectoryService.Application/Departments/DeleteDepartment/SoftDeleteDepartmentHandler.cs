using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Database;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Application.Locations.Interfaces;
using DirectoryService.Application.Positions.Interfaces;
using DirectoryService.Domain.DepartmentLocations;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Departments.SoftDeleteDepartment;

public class SoftDeleteDepartmentHandler(
    IDepartmentsRepository departmentsRepository,
    ILocationsRepository locationsRepository,
    IPositionsRepository positionRepository,
    ITransactionManager transactionManager,
    ILogger<SoftDeleteDepartmentHandler> logger
    ) : ICommandHandler<Guid, SoftDeleteDepartmentCommand>
{
    public async Task<Result<Guid, Errors>> Handle(
        SoftDeleteDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        var transactionResult = await transactionManager.BeginTransaction(cancellationToken);

        if (transactionResult.IsFailure)
        {
            logger.LogError("Failed to begin transaction: {error}", transactionResult.Error);
            return transactionResult.Error.ToErrors();
        }

        using var transactionScope = transactionResult.Value;

        var departmentResult = await departmentsRepository.GetBy(d => d.Id == command.DepartmentId && d.IsActive, cancellationToken);

        if (departmentResult.IsFailure)
        {
            logger.LogError("Department with id {DepartmentId} not found or is not active.", command.DepartmentId);
            return departmentResult.Error.ToErrors();
        }

        var department = departmentResult.Value;

        department.SoftDelete();

        // Если у department есть только одно место, то при удалении department мы удаляем и это место, так как оно не может существовать без department.
        if (department.DepartmentLocations.Count <= 1)
        {
            var departmentLocation = department.DepartmentLocations.First();

            var locationId = departmentLocation.LocationId;

            var locationResult = await locationsRepository.GetBy(l => l.Id == locationId, cancellationToken);

            if (locationResult.IsFailure)
            {
                logger.LogError("Location with id {LocationId} not found.", locationId);
                return locationResult.Error.ToErrors();
        }

            var location = locationResult.Value;
            location.SoftDelete();
        }
        
        if (department.DepartmentPositions.Count <= 1)
        {
            var departmentPosition = department.DepartmentPositions.First();

            var positionId = departmentPosition.PositionId;

            var positionResult = await positionRepository.GetBy(l => l.Id == positionId, cancellationToken);

            if (positionResult.IsFailure)
            {
                logger.LogError("Position with id {PositionId} not found.", positionId);
                return positionResult.Error.ToErrors();
            }

            var position = positionResult.Value;
            position.SoftDelete();
        }

        logger.LogInformation("Department with id {DepartmentId} has been soft deleted.", command.DepartmentId);

        return Result.Success<Guid, Errors>(command.DepartmentId);
    }
}