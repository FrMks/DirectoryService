using CSharpFunctionalExtensions;
using Dapper;
using Shared.Core.Abstractions;
using DirectoryService.Contracts.Departments.GetTopDepartments;
using Microsoft.Extensions.Caching.Hybrid;
using Shared;
using Shared.Core.Database;

namespace DirectoryService.Application.Departments.GetTopDepartments;

public class GetTopDepartmentsDapperHandler(
    IDbConnectionFactory dbConnectionFactory,
    HybridCache cache) : IQueryHandler<Result<DepartmentWithPositionsDapperDto[], Errors>>
{
    public async Task<Result<DepartmentWithPositionsDapperDto[], Errors>> Handle(CancellationToken cancellationToken)
    {
        var topDepartmentsList = await GetTopDepartmentsCachedAsync(cancellationToken);

        return Result.Success<DepartmentWithPositionsDapperDto[], Errors>(topDepartmentsList);
    }

    private async Task<DepartmentWithPositionsDapperDto[]> GetTopDepartmentsCachedAsync(
        CancellationToken cancellationToken)
    {
        var cacheKey = "top_departments_with_positions_dapper";

        return await cache.GetOrCreateAsync(
            cacheKey,
            async _ =>
            {
                var dbConnection = dbConnectionFactory.GetDbConnection();

                var sql = @"
                    SELECT 
                        d.id,
                        d.name,
                        d.identifier,
                        d.parent_id,
                        d.path,
                        d.depth,
                        d.created_at,
                        d.updated_at,
                        CAST(COUNT(dp.id) AS INTEGER) as PositionsCount
                    FROM departments d
                    LEFT JOIN department_positions dp ON d.id = dp.department_id
                    GROUP BY 
                        d.id,
                        d.name,
                        d.identifier,
                        d.parent_id,
                        d.path,
                        d.depth,
                        d.is_active,
                        d.created_at,
                        d.updated_at
                    ORDER BY PositionsCount DESC
                    LIMIT 5;";

                var queryResult = await dbConnection.QueryAsync<DepartmentDto, int, DepartmentWithPositionsDapperDto>(
                    sql,
                    (department, positionsCount) => new DepartmentWithPositionsDapperDto(department, positionsCount),
                    splitOn: "PositionsCount");

                return queryResult.ToArray();
            },
            tags: [CacheTags.DepartmentsList],
            cancellationToken: cancellationToken);
    }
}
