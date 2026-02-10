using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Database;
using DirectoryService.Contracts.Departments.GetTopDepartments;
using DirectoryService.Domain.Department;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Departments.GetTopDepartments;

public class GetTopDepartmentsHandler(
    IReadDbContext readDbContext,
    ILogger<GetTopDepartmentsHandler> logger) : IQueryHandler<Result<TopDepartmentsResponse, Errors>>
{
    public async Task<Result<TopDepartmentsResponse, Errors>> Handle(CancellationToken cancellationToken)
    {
        var emptyList = Enumerable.Empty<Department>();
        var response = new TopDepartmentsResponse(emptyList.ToList());

        return Result.Success<TopDepartmentsResponse, Errors>(response);
    }
}