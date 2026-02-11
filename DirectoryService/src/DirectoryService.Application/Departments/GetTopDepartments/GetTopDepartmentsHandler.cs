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
    ILogger<GetTopDepartmentsHandler> logger) : IQueryHandler<Result<TopDepartmentsResponse, Errors>>
{
    public async Task<Result<TopDepartmentsResponse, Errors>> Handle(CancellationToken cancellationToken)
    {
        var departments = readDbContext.DepartmentsRead;

        var topDepartments = departments
            .Select(d => new
            {
                Department = d,
                PositionsCount = d.DepartmentPositions.Count(),
            })
            .OrderByDescending(d => d.PositionsCount)
            .Take(5);

        List<DepartmentWithPositionsDto> topDepartmentsList = await topDepartments
            .Select(d => new DepartmentWithPositionsDto(d.Department, d.PositionsCount))
            .ToListAsync(cancellationToken);

        var response = new TopDepartmentsResponse(topDepartmentsList);

        return Result.Success<TopDepartmentsResponse, Errors>(response);
    }
}