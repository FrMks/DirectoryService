using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Database;
using DirectoryService.Contracts.Departments.GetDisclosureOfDepartments;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Departments.GetDisclosureOfDepartments;

public class GetDepartmentsWithLazyLoadingOfChildrenHandler(
    IReadDbContext readDbContext,
    ILogger<GetDepartmentsWithLazyLoadingOfChildrenHandler> logger)
    : IQueryHandler<Result<DepartmentDtoWithLazyLoadingOfChildren[], Errors>>
{
    public Task<Result<DepartmentDtoWithLazyLoadingOfChildren[], Errors>> Handle(CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success<DepartmentDtoWithLazyLoadingOfChildren[], Errors>(Array.Empty<DepartmentDtoWithLazyLoadingOfChildren>()));
    }
}