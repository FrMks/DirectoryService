using System.Windows.Markup;
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
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.ToList())
            {
                logger.LogError("Validation error when soft deleting department: {error}", error.Message);
            }

            return validationResult.ToList();
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
