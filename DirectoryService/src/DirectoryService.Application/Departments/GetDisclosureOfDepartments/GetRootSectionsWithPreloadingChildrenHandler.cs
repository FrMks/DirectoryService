using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Database;
using DirectoryService.Contracts.Departments.GetDisclosureOfDepartments;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Departments.GetDisclosureOfDepartments;

public class GetRootSectionsWithPreloadingChildrenHandler(
    IReadDbContext readDbContext,
    ILogger<GetRootSectionsWithPreloadingChildrenHandler> logger)
    : IQueryHandler<Result<DepartmentDtoWithPreloadingChildren[], Errors>>
{
    public Task<Result<DepartmentDtoWithPreloadingChildren[], Errors>> Handle(CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success<DepartmentDtoWithPreloadingChildren[], Errors>(Array.Empty<DepartmentDtoWithPreloadingChildren>()));
    }
}