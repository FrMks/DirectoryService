using DirectoryService.Application.Database;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Department.ValueObject;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Infrastructure.Postgres.Services;

public class DepartmentCleanupService
{
    private readonly IDepartmentsRepository _departmentsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<DepartmentCleanupService> _logger;

    public DepartmentCleanupService(
        IDepartmentsRepository departmentsRepository,
        ITransactionManager transactionManager,
        ILogger<DepartmentCleanupService> logger)
    {
        _departmentsRepository = departmentsRepository;
        _transactionManager = transactionManager;
        _logger = logger;
    }

    public async Task CleanupInactiveDepartments(int inactiveDaysThreshold, CancellationToken stoppingToken)
    {
        var thresholdDate = DateTime.UtcNow.AddDays(-inactiveDaysThreshold);

        // Получаем неактивные департаменты, которые были удалены и дата удаления которых меньше или равна thresholdDate
        var departmentsToRemoveResult = await _departmentsRepository.GetListBy(
            d => d.IsActive == false
            && d.DeletedAt != null
            && d.DeletedAt <= thresholdDate,
            stoppingToken);

        if (departmentsToRemoveResult.IsFailure)
        {
            if (departmentsToRemoveResult.Error.Code == "departments.not.found")
            {
                _logger.LogInformation("No inactive departments found for cleanup.");
                return;
            }

            _logger.LogError(
                "Failed to retrieve inactive departments for cleanup: {ErrorCode} {ErrorMessage}",
                departmentsToRemoveResult.Error.Code,
                departmentsToRemoveResult.Error.Message);
            return;
        }

        if (!departmentsToRemoveResult.Value.Any())
        {
            _logger.LogInformation("No inactive departments found for cleanup.");
            return;
        }

        var departmentsToRemove = departmentsToRemoveResult.Value;

        // Создаем список кандидатов на удаление, который содержит Id, Path и ParentId для каждого неактивного департамента
        List<DepartmentIdPathAndParentId> candidates = departmentsToRemove
            .Select(d => new DepartmentIdPathAndParentId(d.Id, d.Path, d.ParentId.HasValue ? DepartmentId.FromValue(d.ParentId.Value) : null))
            .ToList();

        var transactionResult = await _transactionManager.BeginTransaction(stoppingToken);
        if (transactionResult.IsFailure)
        {
            _logger.LogError(
                "Failed to begin transaction for department cleanup: {ErrorCode} {ErrorMessage}",
                transactionResult.Error.Code,
                transactionResult.Error.Message);
            return;
        }

        // Используем using для обеспечения правильного завершения транзакции
        using var transactionScope = transactionResult.Value;

        // Вызываем метод репозитория для удаления департаментов и их детей, передавая список кандидатов на удаление
        _logger.LogInformation(
            "Starting cleanup of {DepartmentsCount} inactive departments.",
            candidates.Count);
        var cleanupResult = await _departmentsRepository.CleanupDeletedDepartmentsOlderThan(candidates, stoppingToken);
        if (cleanupResult.IsFailure)
        {
            _logger.LogError(
                "Failed to cleanup deleted departments: {ErrorCode} {ErrorMessage}",
                cleanupResult.Error.Code,
                cleanupResult.Error.Message);
            return;
        }

        _logger.LogInformation(
            "Successfully cleaned up {DepartmentsCount} inactive departments. Saving changes.",
            candidates.Count);

        var saveChangesResult = await _transactionManager.SaveChangesAsync(stoppingToken);
        if (saveChangesResult.IsFailure)
        {
            _logger.LogError(
                "Failed to save cleanup changes: {ErrorCode} {ErrorMessage}",
                saveChangesResult.Error.Code,
                saveChangesResult.Error.Message);
            return;
        }

        var commitResult = transactionScope.Commit();
        if (commitResult.IsFailure)
        {
            _logger.LogError(
                "Failed to commit cleanup transaction: {ErrorCode} {ErrorMessage}",
                commitResult.Error.Code,
                commitResult.Error.Message);
            return;
        }

        _logger.LogInformation(
            "Department cleanup completed successfully for {DepartmentsCount} departments.",
            candidates.Count);
    }
}
