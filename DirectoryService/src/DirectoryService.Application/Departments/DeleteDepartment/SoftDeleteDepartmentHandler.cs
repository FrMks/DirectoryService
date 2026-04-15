using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Caching;
using DirectoryService.Application.Database;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Application.Extensions;
using DirectoryService.Application.Locations.Interfaces;
using DirectoryService.Application.Positions.Interfaces;
using DirectoryService.Domain;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.DepartmentLocations;
using FluentValidation;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.Extensions.Caching.Hybrid;
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
    HybridCache cache,
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

        var softDeleteLocationsResult = await locationsRepository
            .SoftDeleteUnusedLocationsInBranchAsync(department.Path, cancellationToken);
        if (softDeleteLocationsResult.IsFailure)
            return softDeleteLocationsResult.Error.ToErrors();

        var softDeletePositionsResult = await positionRepository
            .SoftDeleteUnusedPositionsInBranchAsync(department.Path, cancellationToken);
        if (softDeletePositionsResult.IsFailure)
            return softDeletePositionsResult.Error.ToErrors();

        var oldPath = department.Path;
        var newPathResult = Path.CreateDeletedBranchPath(department.Path.Value);
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

        await cache.RemoveByTagAsync(CacheTags.DepartmentsList, cancellationToken);

        // Возвращаем успешный результат с ID удаленного департамента
        return Result.Success<Guid, Errors>(command.DepartmentId);
    }
}
