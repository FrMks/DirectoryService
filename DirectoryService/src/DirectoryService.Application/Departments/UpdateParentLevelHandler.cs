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
                logger.LogInformation("Error when updating parent level, error: {error}", error.Message);
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
            var error = Error.Validation(
                "department.id.equals.level.id",
                $"department id: {command.DepartmentId} and level id equal to each other");
            logger.LogInformation("Error when updating parent level, error: {error}", error.Message);
            return error.ToErrors();
        }

        // Проверили, что id department, который мы хотим переместить на другое место существует
        DepartmentId departmentId = DepartmentId.FromValue(command.DepartmentId);
        // TODO: должен ли быть активным id департамента, который мы хотим перенести?
        var actualDepartmentResult = await departmentsRepository.GetByIdActiveDepartmentWithLock(departmentId, cancellationToken);
        if (actualDepartmentResult.IsFailure)
        {
            logger.LogInformation(
                "Error when try get by id actual department, error: {error}",
                actualDepartmentResult.Error);
            return actualDepartmentResult.Error.ToErrors();
        }

        if (command.ParentLevelRequest.ParentDepartmentId == null)
        {
            // Ставим как родителя
        }
        else
        {
            // Переносим под родителя с всеми его детьми
        }

        transactionScope.Commit();

        return Result.Success<Guid, Errors>(Guid.NewGuid());
    }
}