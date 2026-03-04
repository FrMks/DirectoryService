using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Database;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Application.Locations.Interfaces;
using DirectoryService.Application.Positions.Interfaces;
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
        var departmentResult = await departmentsRepository.GetBy(d => d.Id == command.DepartmentId && d.IsActive, cancellationToken);

        if (departmentResult.IsFailure)
        {
            logger.LogError("Department with id {DepartmentId} not found or is not active.", command.DepartmentId);
            return departmentResult.Error.ToErrors();
        }

        var department = departmentResult.Value;

        // Выполняем мягкое удаление департамента
        department.SoftDelete();

        // Обработка удаления связанного места, если оно единственное
        // Место не может существовать без департамента
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
        
        // Обработка удаления связанной должности, если она единственная
        // Должность не может существовать без департамента
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

        var identifier = department.Identifier.Value;

        // Получаем дочерние департаменты, которые нужно обновить
        var departmentsToChangePathResult = await departmentsRepository
            .GetListBy(
                d => d.Path.Value == identifier
                || d.Path.Value.StartsWith(identifier + ".")
                || d.Path.Value.EndsWith("." + identifier)
                || d.Path.Value.Contains("." + identifier + "."),
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
}