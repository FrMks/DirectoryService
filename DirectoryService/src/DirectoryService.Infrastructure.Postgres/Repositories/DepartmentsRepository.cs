using CSharpFunctionalExtensions;
using DirectoryService.Application.Departments.Interfaces;
using DirectoryService.Domain.Department;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Infrastructure.Postgres.Repositories;

public class DepartmentsRepository(DirectoryServiceDbContext dbContext, ILogger<DepartmentsRepository> logger)
    : IDepartmentsRepository
{
    public Task<Result<Guid, Error>> AddAsync(Department department, CancellationToken cancellationToken)
    {
        
    }
}