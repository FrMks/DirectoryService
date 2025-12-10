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
        var transactionScopeResult =
            await transactionManager.BeginTransactionAsTask(cancellationToken, IsolationLevel.RepeatableRead);
        if (transactionScopeResult.IsFailure)
        {
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
        // TODO: должен ли быть активным id департамента, который мы хотим перенести?
        if (actualDepartmentResult.Value.IsActive == false)
        {
            transactionScope.Rollback();
            logger.LogError(
                "Error when taking department, because department is not active");
            return Error.Failure(
                "department.is.not.active",
                "Error when taking department, because department is not active").ToErrors();
        }
        
        var actualDepartment = actualDepartmentResult.Value;
        
        // Получаем список состоящий из самого департамента и его детей
        var parentWithChildrenResult = await departmentsRepository.GetDepartmentWithChildren(actualDepartment.Path, cancellationToken);
        if (parentWithChildrenResult.IsFailure)
        {
            transactionScope.Rollback();
            logger.LogError(
                "When getting parent with children in from department return error {error}",
                parentWithChildrenResult.Error);
            return parentWithChildrenResult.Error;
        }

        var parentWithChildren = parentWithChildrenResult.Value;
        
        var oldPath = actualDepartment.Path;
        var oldDepth = actualDepartment.Depth;

        if (command.ParentLevelRequest.ParentDepartmentId == null)
        {
            // Ставим как родителя
        }
        else
        {
            // Переносим под родителя с всеми его детьми
            
            // Проверяем, что куда мы хотим перетащить не находится внутри списка самого департамента и его детей
            var idWhereToMove = DepartmentId.FromValue(command.ParentLevelRequest.ParentDepartmentId);
            var departmentWhereToMove = await departmentsRepository.GetByIdWithLock(idWhereToMove, cancellationToken);
            if (departmentWhereToMove.IsFailure)
            {
                logger.LogError("When getting department by id return error: {error}", departmentWhereToMove.Error);
                return departmentWhereToMove.Error.ToErrors();
            }

            if (parentWithChildren.Contains(departmentWhereToMove.Value))
            {
                logger.LogError(
                    "Department with id: {departmentWhereToMove.Value.Id} have in collection of children and department",
                    departmentWhereToMove.Value.Id);
                return Error.Failure(
                    "department.have.in.collection",
                    $"Department with id: {departmentWhereToMove.Value.Id} " +
                    $"have in collection of children and department").ToErrors();
            }
        }

        transactionScope.Commit();

        return Result.Success<Guid, Errors>(Guid.NewGuid());
    }
}