using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Database;
using DirectoryService.Contracts.Departments.GetTopDepartments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Departments.GetTopDepartments;

public class GetTopDepartmentsHandler(
    IReadDbContext readDbContext,
    ILogger<GetTopDepartmentsHandler> logger) : IQueryHandler<Result<DepartmentWithPositionsDto[], Errors>>
{
    public async Task<Result<DepartmentWithPositionsDto[], Errors>> Handle(CancellationToken cancellationToken)
    {
        var topDepartmentsList = await readDbContext.DepartmentsRead
        .OrderByDescending(d => d.DepartmentPositions.Count())
        .Take(5)
        .Select(d => new DepartmentWithPositionsDto(
            new DepartmentDto
            {
                Id = d.Id.Value,
                Name = d.Name.Value,
                Identifier = d.Identifier.Value,
                ParentId = d.ParentId,
                Path = d.Path.Value,
                Depth = d.Depth.Value,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            },
            d.DepartmentPositions.Count()
        ))
        .ToListAsync(cancellationToken);

        return Result.Success<DepartmentWithPositionsDto[], Errors>(topDepartmentsList.ToArray());
    }
}