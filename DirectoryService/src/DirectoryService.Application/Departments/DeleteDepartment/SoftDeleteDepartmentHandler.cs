using System.Windows.Markup;
using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Database;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Application.Locations.Interfaces;
using DirectoryService.Application.Positions.Interfaces;
using DirectoryService.Domain;
using DirectoryService.Domain.Department;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.Locations;
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
        // Начинаем транзакцию для атомарности операции
        var transactionResult = await transactionManager.BeginTransaction(cancellationToken);

        if (transactionResult.IsFailure)
        {
            logger.LogError("Failed to begin transaction: {error}", transactionResult.Error);
            return transactionResult.Error.ToErrors();
        }

        using var transactionScope = transactionResult.Value;

        // Получаем активный департамент по ID
        var departmentResult = await departmentsRepository
            .GetActiveDepartmentForSoftDelete(DepartmentId.FromValue(command.DepartmentId), cancellationToken);

        if (departmentResult.IsFailure)
        {
            logger.LogError("Department with id {DepartmentId} not found or is not active.", command.DepartmentId);
            return departmentResult.Error.ToErrors();
        }

        var department = departmentResult.Value;

        // Выполняем мягкое удаление департамента
        department.SoftDelete();

        var processLocationsResult = await ProcessLocationsAsync(
            department.Id,
            department.DepartmentLocations,
            cancellationToken);
        if (processLocationsResult.IsFailure)
            return processLocationsResult.Error;

        var processPositionsResult = await ProcessPositionsAsync(
            department.Id,
            department.DepartmentPositions,
            cancellationToken);
        if (processPositionsResult.IsFailure)
            return processPositionsResult.Error;

        var identifier = department.Identifier.Value;

        // Получаем дочерние департаменты, которые нужно обновить
        var departmentsToChangePathResult = await departmentsRepository
            .GetListBy(
                d => d.Id != department.Id &&
                (d.Path.Value == identifier
                || d.Path.Value.StartsWith(identifier + ".")
                || d.Path.Value.EndsWith("." + identifier)
                || d.Path.Value.Contains("." + identifier + ".")),
                cancellationToken);
        
        if (departmentsToChangePathResult.IsFailure)
        {
            logger.LogError("Failed to retrieve child departments for department with id {DepartmentId}.", command.DepartmentId);
            return departmentsToChangePathResult.Error.ToErrors();
        }

        var departmentsToChangePath = departmentsToChangePathResult.Value;

        // Обновляем пути для удаленного департамента и его дочерних элементов
        var parentNewPath = department.Path.Value.Replace(identifier, $"deleted-{identifier}");
        var parentUpdateResult = department.UpdatePath(parentNewPath);

        if (parentUpdateResult.IsFailure)
        {
            logger.LogError("Failed to update path in parent department.");
            return parentUpdateResult.Error.ToErrors();
        }

        // Обновляем пути для всех дочерних департаментов
        foreach (var childDepartment in departmentsToChangePath)
        {
            var childNewPath = childDepartment.Path.Value.Replace(identifier, $"deleted-{identifier}");
            childDepartment.UpdatePath(childNewPath);
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            logger.LogError("Failed to save changes.");
            return saveResult.Error.ToErrors();
        }

        logger.LogInformation("Department with id {DepartmentId} has been soft deleted.", command.DepartmentId);

        transactionScope.Commit();

        // Возвращаем успешный результат с ID удаленного департамента
        return Result.Success<Guid, Errors>(command.DepartmentId);
    }

    private async Task<UnitResult<Errors>> ProcessLocationsAsync(
        Guid deletingDepartmentId,
        IReadOnlyList<DepartmentLocation> departmentLocations,
        CancellationToken cancellationToken)
    {
        foreach (var departmentLocation in departmentLocations)
        {
            var locationId = departmentLocation.LocationId;

            var locationResult = await locationsRepository
                .GetBy(l => l.Id == locationId, cancellationToken);

            if (locationResult.IsFailure)
            {
                logger.LogError("Location with id {LocationId} not found.", locationId);
                return locationResult.Error.ToErrors();
            }

            var location = locationResult.Value;

            var hasOtherActiveDepartmentsResult = await locationsRepository
                .HasOtherActiveDepartmentsForLocation(
                    locationId,
                    DepartmentId.FromValue(deletingDepartmentId),
                    cancellationToken);
            if (hasOtherActiveDepartmentsResult.IsFailure)
            {
                logger.LogError(
                    "Failed to check other active departments for location {LocationId}.",
                    locationId.Value);
                return hasOtherActiveDepartmentsResult.Error.ToErrors();
            }

            if (!hasOtherActiveDepartmentsResult.Value)
            {
                location.SoftDelete();
            }
        }

        return UnitResult.Success<Errors>();
    }

    private async Task<UnitResult<Errors>> ProcessPositionsAsync(
        Guid deletingDepartmentId,
        IReadOnlyList<DepartmentPosition> departmentPositions,
        CancellationToken cancellationToken)
    {
        foreach (var departmentPosition in departmentPositions)
        {
            var positionId = departmentPosition.PositionId;

            var positionResult = await positionRepository
                .GetBy(p => p.Id == positionId, cancellationToken);

            if (positionResult.IsFailure)
            {
                logger.LogError("Position with id {PositionId} not found.", positionId);
                return positionResult.Error.ToErrors();
            }

            var position = positionResult.Value;

            var hasOtherActiveDepartmentsResult = await positionRepository
                .HasOtherActiveDepartmentsForPosition(
                    positionId,
                    DepartmentId.FromValue(deletingDepartmentId),
                    cancellationToken);
            if (hasOtherActiveDepartmentsResult.IsFailure)
            {
                logger.LogError(
                    "Failed to check other active departments for position {PositionId}.",
                    positionId.Value);
                return hasOtherActiveDepartmentsResult.Error.ToErrors();
            }

            if (!hasOtherActiveDepartmentsResult.Value)
            {
                position.SoftDelete();
            }
        }

        return UnitResult.Success<Errors>();
    }
}
