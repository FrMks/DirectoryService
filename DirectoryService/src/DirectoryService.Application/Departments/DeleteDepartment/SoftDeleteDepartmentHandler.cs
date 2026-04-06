using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Database;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Application.Extensions;
using DirectoryService.Application.Locations.Interfaces;
using DirectoryService.Application.Positions.Interfaces;
using DirectoryService.Domain;
using DirectoryService.Domain.Department;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.DepartmentLocations;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared;
using Path = DirectoryService.Domain.Department.ValueObject.Path;

namespace DirectoryService.Application.Departments.SoftDeleteDepartment;

public class SoftDeleteDepartmentHandler(
    IDepartmentsRepository departmentsRepository,
    ILocationsRepository locationsRepository,
    IPositionsRepository positionRepository,
    IValidator<SoftDeleteDepartmentCommand> validator,
    ITransactionManager transactionManager,
    ILogger<SoftDeleteDepartmentHandler> logger
    ) : ICommandHandler<Guid, SoftDeleteDepartmentCommand>
{
    public async Task<Result<Guid, Errors>> Handle(
        SoftDeleteDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        var validationErrors = await validator.GetValidationErrorsAsync(command, cancellationToken);
        if (validationErrors is not null)
        {
            logger.LogErrors(validationErrors, "Validation error when soft deleting department");
            return validationErrors;
        }

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

        var oldPath = department.Path;
        var newPathResult = CreateDeletedBranchPath(department.Path.Value);
        if (newPathResult.IsFailure)
        {
            logger.LogError(
                "Failed to create deleted path for department with id {DepartmentId}.",
                command.DepartmentId);
            return newPathResult.Error.ToErrors();
        }

        var moveBranchResult = await departmentsRepository.MoveDepartmentWithChildren(
            oldPath,
            newPathResult.Value,
            department.ParentId,
            cancellationToken);
        if (moveBranchResult.IsFailure)
        {
            logger.LogError(
                "Failed to update deleted branch path for department with id {DepartmentId}.",
                command.DepartmentId);
            return moveBranchResult.Error.ToErrors();
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
        var locationIds = departmentLocations
            .Select(dl => dl.LocationId)
            .Distinct()
            .ToList();

        var locationsResult = await locationsRepository.GetLocationsByIds(locationIds, cancellationToken);
        if (locationsResult.IsFailure)
        {
            logger.LogError("Failed to retrieve locations for department with id {DepartmentId}.", deletingDepartmentId);
            return locationsResult.Error.ToErrors();
        }

        var locationIdsWithOtherActiveDepartmentsResult = await locationsRepository
            .GetLocationIdsWithOtherActiveDepartments(
                locationIds,
                DepartmentId.FromValue(deletingDepartmentId),
                cancellationToken);
        if (locationIdsWithOtherActiveDepartmentsResult.IsFailure)
        {
            logger.LogError(
                "Failed to retrieve location ids with other active departments for department with id {DepartmentId}.",
                deletingDepartmentId);
            return locationIdsWithOtherActiveDepartmentsResult.Error.ToErrors();
        }

        var locationIdsWithOtherActiveDepartments = locationIdsWithOtherActiveDepartmentsResult.Value;

        foreach (var location in locationsResult.Value)
        {
            if (!locationIdsWithOtherActiveDepartments.Contains(location.Id))
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
        if (departmentPositions.Count == 0)
        {
            return UnitResult.Success<Errors>();
        }

        var positionIds = departmentPositions
            .Select(dp => dp.PositionId)
            .Distinct()
            .ToList();

        var positionsResult = await positionRepository.GetPositionsByIds(positionIds, cancellationToken);
        if (positionsResult.IsFailure)
        {
            logger.LogError("Failed to retrieve positions for department with id {DepartmentId}.", deletingDepartmentId);
            return positionsResult.Error.ToErrors();
        }

        var positionIdsWithOtherActiveDepartmentsResult = await positionRepository
            .GetPositionIdsWithOtherActiveDepartments(
                positionIds,
                DepartmentId.FromValue(deletingDepartmentId),
                cancellationToken);
        if (positionIdsWithOtherActiveDepartmentsResult.IsFailure)
        {
            logger.LogError(
                "Failed to retrieve position ids with other active departments for department with id {DepartmentId}.",
                deletingDepartmentId);
            return positionIdsWithOtherActiveDepartmentsResult.Error.ToErrors();
        }

        var positionIdsWithOtherActiveDepartments = positionIdsWithOtherActiveDepartmentsResult.Value;

        foreach (var position in positionsResult.Value)
        {
            if (!positionIdsWithOtherActiveDepartments.Contains(position.Id))
            {
                position.SoftDelete();
            }
        }

        return UnitResult.Success<Errors>();
    }

    private Result<Path, Error> CreateDeletedBranchPath(string currentPath)
    {
        var segments = currentPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return Error.Validation("department.path.invalid", "Department path is invalid.");

        segments[^1] = $"deleted-{segments[^1]}";
        var updatedPath = string.Join('.', segments);

        return Path.Create(updatedPath);
    }
}
