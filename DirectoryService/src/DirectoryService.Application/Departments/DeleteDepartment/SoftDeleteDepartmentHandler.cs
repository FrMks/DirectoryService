using System.Windows.Markup;
using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Database;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Application.Locations.Interfaces;
using DirectoryService.Application.Positions.Interfaces;
using DirectoryService.Domain;
using DirectoryService.Domain.Department;
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
            .GetBy(d => d.Id == command.DepartmentId && d.IsActive, cancellationToken);

        if (departmentResult.IsFailure)
        {
            logger.LogError("Department with id {DepartmentId} not found or is not active.", command.DepartmentId);
            return departmentResult.Error.ToErrors();
        }

        var department = departmentResult.Value;

        // Выполняем мягкое удаление департамента
        department.SoftDelete();

        await ProcessLocationsAsync(
            department.Id,
            department.DepartmentLocations,
            cancellationToken);

        await ProcessPositionsAsync(
            department.Id,
            department.DepartmentPositions,
            cancellationToken);

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

        // Возвращаем успешный результат с ID удаленного департамента
        return Result.Success<Guid, Errors>(command.DepartmentId);
    }

    private async Task ProcessLocationsAsync(
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
                continue;
            }

            var location = locationResult.Value;

            var hasOtherActiveDepartments = location.DepartmentLocations
                .Any(dl => dl.DepartmentId != deletingDepartmentId);

            if (!hasOtherActiveDepartments)
            {
                location.SoftDelete();
            }
        }
    }

    private async Task ProcessPositionsAsync(
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
                continue;
            }

            var position = positionResult.Value;

            var hasOtherActiveDepartments = position.DepartmentPositions
                .Any(dp => dp.DepartmentId != deletingDepartmentId);

            if (!hasOtherActiveDepartments)
            {
                position.SoftDelete();
            }
        }
    }
}