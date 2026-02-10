using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Database;
using DirectoryService.Contracts.Departments.GetTopDepartments;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Departments.GetTopDepartments;

public class GetTopDepartmentsHandler(
    IReadDbContext readDbContext,
    ILogger<GetTopDepartmentsHandler> logger) : IQueryHandler<GetTopDepartmentsQuery, Result<TopDepartmentsResponse, Errors>>
{
    public Task<Result<TopDepartmentsResponse, Errors>> Handle(GetTopDepartmentsQuery query, CancellationToken cancellationToken)
    {

    }
}