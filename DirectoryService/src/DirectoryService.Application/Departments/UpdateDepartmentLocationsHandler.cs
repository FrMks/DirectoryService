using CSharpFunctionalExtensions;
using Shared;

namespace DirectoryService.Application.Departments;

public class UpdateDepartmentLocationsHandler
{
    public async Task<Result<Guid, Errors>> Handle(
        UpdateLocationsCommand command,
        CancellationToken cancellationToken)
    {
        
    }
}