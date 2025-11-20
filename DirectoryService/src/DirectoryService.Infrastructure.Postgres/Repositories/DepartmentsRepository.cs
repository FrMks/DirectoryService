using CSharpFunctionalExtensions;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Domain.Department;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Infrastructure.Postgres.Repositories;

public class DepartmentsRepository(DirectoryServiceDbContext dbContext, ILogger<DepartmentsRepository> logger)
    : IDepartmentsRepository
{
    public async Task<Result<Guid, Error>> AddAsync(Department department, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Departments.AddAsync(department, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken); // Применяем изменения
        }
        catch (Exception e)
        {
            return Error.Failure(null, "Database error occurred.");
        }
        
        return Result.Success<Guid, Error>(department.Id.Value);
    }
}