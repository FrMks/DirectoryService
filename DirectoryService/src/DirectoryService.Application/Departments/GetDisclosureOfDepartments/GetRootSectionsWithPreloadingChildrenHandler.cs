using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Application.Abstractions;
using DirectoryService.Contracts.Departments.GetDisclosureOfDepartments;
using DirectoryService.Contracts.Departments.GetTopDepartments;
using Shared;
using Shared.Database;

namespace DirectoryService.Application.Departments.GetDisclosureOfDepartments;

public class GetRootSectionsWithPreloadingChildrenHandler(
    IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GettingRootSectionsWithPreloadingChildrenRequest,
            Result<DepartmentDtoWithPreloadingChildren[], Errors>>
{
    public async Task<Result<DepartmentDtoWithPreloadingChildren[], Errors>> Handle(
        GettingRootSectionsWithPreloadingChildrenRequest query,
        CancellationToken cancellationToken)
    {
        var page = query.Pagination?.Page < 1 ? 1 : query.Pagination?.Page ?? 1;
        var pageSize = query.Pagination?.PageSize < 1 ? 20 : query.Pagination?.PageSize ?? 20;
        var offset = (page - 1) * pageSize;
        var prefetch = query.Prefetch < 1 ? 3 : query.Prefetch ?? 3;

        var dbConnection = dbConnectionFactory.GetDbConnection();

        var parameters = new
        {
            offset = offset,
            root_limit = pageSize,
            prefetch = prefetch,
        };

        var sql = @"
            WITH roots AS (
                 SELECT
                    d.id,
                    d.name,
                    d.identifier,
                    d.parent_id AS ParentId,
                    d.path,
                    d.depth,
                    d.is_active AS IsActive,
                    d.created_at AS CreatedAt,
                    d.updated_at AS UpdatedAt
                FROM departments AS d
                WHERE d.parent_id IS NULL AND d.is_active = true
                ORDER BY d.created_at
                OFFSET @offset LIMIT @root_limit
            )

            -- Получаем родительские подразделения
            SELECT *,
             (EXISTS(
                SELECT 1 FROM departments
                WHERE parent_id = roots.id
                AND is_active = true
                OFFSET @prefetch LIMIT 1)) AS HasMoreChildren 
            FROM roots

            UNION ALL

            -- Получаем дочерние подразделения для каждого родительского подразделения
            SELECT c.*,
                (EXISTS(
                    SELECT 1 FROM departments 
                    WHERE parent_id = c.id AND is_active = true)) AS HasMoreChildren
            FROM roots AS r
            CROSS JOIN LATERAL (
                SELECT
                    d.id,
                    d.name,
                    d.identifier,
                    d.parent_id,
                    d.path,
                    d.depth,
                    d.is_active,
                    d.created_at,
                    d.updated_at
                FROM departments AS d
                WHERE d.parent_id = r.id AND d.is_active = true
                ORDER BY d.created_at
                LIMIT @prefetch) AS c
                ";

        var rawResults = await dbConnection.QueryAsync<DepartmentRawDto>(sql, parameters);

        var departments = rawResults.Select(r => new DepartmentDtoWithPreloadingChildren
        (
            Department: new DepartmentDto
            {
                Id = r.Id,
                Name = r.Name,
                Identifier = r.Identifier,
                ParentId = r.ParentId,
                Path = r.Path,
                Depth = r.Depth,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            },
            HasMoreChildren: r.HasMoreChildren
        )).ToArray();

        return Result.Success<DepartmentDtoWithPreloadingChildren[], Errors>(departments);
    }
}
