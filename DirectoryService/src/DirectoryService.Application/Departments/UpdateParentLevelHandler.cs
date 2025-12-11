using System.Data;
using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Database;
using DirectoryService.Application.Departments;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Application.Extensions;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Department.ValueObject;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared;
using Path = DirectoryService.Domain.Department.ValueObject.Path;

public class UpdateParentLevelHandler(
    IDepartmentsRepository departmentsRepository,
    IValidator<UpdateParentLevelRequest> validator,
    ITransactionManager transactionManager,
    ILogger<UpdateParentLevelHandler> logger)
    : ICommandHandler<Guid, UpdateParentLevelCommand>
{
    public async Task<Result<Guid, Errors>> Handle(UpdateParentLevelCommand command, CancellationToken cancellationToken)
    {
        // Валидация DTO
        var validationResult = await validator.ValidateAsync(command.ParentLevelRequest, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (Error error in validationResult.ToList())
            {
                logger.LogError("Error when updating parent level, error: {error}", error.Message);
            }

            return validationResult.ToList();
        }

        // Работа с транзакциями
        var transactionScopeResult = await transactionManager.
            BeginTransactionAsTask(cancellationToken, IsolationLevel.RepeatableRead);
        if (transactionScopeResult.IsFailure)
        {
            logger.LogError("Failed to begin transaction: {error}", transactionScopeResult.Error);
            return transactionScopeResult.Error.ToErrors();
        }

        using var transactionScope = transactionScopeResult.Value;

        // Проверили, что id места куда хотим поместить не совпадает с тем что хотим перенести
        if (command.DepartmentId == command.ParentLevelRequest.ParentDepartmentId)
        {
            transactionScope.Rollback();
            var error = Error.Validation(
                "department.id.equals.level.id",
                $"department id: {command.DepartmentId} and level id equal to each other");
            logger.LogInformation("Error when updating parent level, error: {error}", error.Message);
            return error.ToErrors();
        }

        // Проверили, что id department, который мы хотим переместить на другое место существует
        DepartmentId departmentId = DepartmentId.FromValue(command.DepartmentId);
        var actualDepartmentResult = await departmentsRepository.GetByIdWithLock(departmentId, cancellationToken);
        if (actualDepartmentResult.IsFailure)
        {
            transactionScope.Rollback();
            logger.LogError(
                "Error when try get by id actual department, error: {error}",
                actualDepartmentResult.Error);
            return actualDepartmentResult.Error.ToErrors();
        }

        var actualDepartment = actualDepartmentResult.Value;

        if (!actualDepartment.IsActive)
        {
            transactionScope.Rollback();
            logger.LogError(
                "Error when taking department, because department is not active");
            return Error.Failure(
                "department.is.not.active",
                "Error when taking department, because department is not active").ToErrors();
        }

        var oldPath = actualDepartment.Path;

        if (command.ParentLevelRequest.ParentDepartmentId == null)
        {
            (_, bool isFailure, Path? newValue, Error? error) = Path.Create(actualDepartment.Identifier.Value);
            if (isFailure)
            {
                transactionScope.Rollback();
                logger.LogError(
                    "When creating path return error {error}",
                    error);
                return error.ToErrors();
            }

            // Ставим как родителя
            var moveResult = await departmentsRepository.MoveDepartmentWithChildren(
                oldPath.Value,
                newValue.Value,
                command.ParentLevelRequest.ParentDepartmentId,
                cancellationToken);
            if (moveResult.IsFailure)
            {
                transactionScope.Rollback();
                logger.LogError("Failed to move department: {error}", moveResult.Error);
                return moveResult.Error.ToErrors();
            }
        }
        else
        {
            // Переносим под родителя с всеми его детьми
            
            // Проверяем, что куда мы хотим перетащить не находится внутри списка самого департамента и его детей
            var newParentId = DepartmentId.FromValue(command.ParentLevelRequest.ParentDepartmentId);
            var newParentResult = await departmentsRepository.GetByIdWithLock(newParentId, cancellationToken);
            if (newParentResult.IsFailure)
            {
                transactionScope.Rollback();
                logger.LogError("When getting department by id return error: {error}", newParentResult.Error);
                return newParentResult.Error.ToErrors();
            }

            var newParent = newParentResult.Value;
            
            if (newParent.Path.Value.StartsWith(oldPath.Value))
            {
                transactionScope.Rollback();
                logger.LogError(
                    "Cannot move department {deptId} to its descendant {parentId}",
                    departmentId.Value,
                    newParentId.Value);
                return Error.Validation(
                    "department.move.to.descendant",
                    "Cannot move department to its own descendant").ToErrors();
            }
            
            var newPathResult = newParent.Path.CreateChild(actualDepartment.Identifier);
            
            var moveResult = await departmentsRepository.MoveDepartmentWithChildren(
                oldPath.Value,
                newPathResult.Value,
                newParent.Id.Value,
                cancellationToken);
            if (moveResult.IsFailure)
            {
                transactionScope.Rollback();
                logger.LogError("Failed to move department: {error}", moveResult.Error);
                return moveResult.Error.ToErrors();
            }
        }

        transactionScope.Commit();

        return Result.Success<Guid, Errors>(Guid.NewGuid());
    }
}