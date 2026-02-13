using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Application.Abstractions;
using DirectoryService.Contracts.Departments.GetDisclosureOfDepartments;
using DirectoryService.Contracts.Departments.GetTopDepartments;
using Shared;
using Shared.Database;

namespace DirectoryService.Application.Departments.GetDisclosureOfDepartments;

public class GetDepartmentsWithLazyLoadingOfChildrenHandler(
    IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetDepartmentWithLazyLoadingOfChildrenRequest,
             Result<DepartmentDtoWithLazyLoadingOfChildren[], Errors>>
{
    public async Task<Result<DepartmentDtoWithLazyLoadingOfChildren[], Errors>> Handle(
        GetDepartmentWithLazyLoadingOfChildrenRequest query,
        CancellationToken cancellationToken)
    {
        var page = query.Pagination?.Page < 1 ? 1 : query.Pagination?.Page ?? 1;
        var pageSize = query.Pagination?.PageSize < 1 ? 20 : query.Pagination?.PageSize ?? 20;
        var offset = (page - 1) * pageSize;

        var dbConnection = dbConnectionFactory.GetDbConnection();

        var parameters = new
        {
            parent_id = query.DepartmentId,
            offset = offset,
            limit = pageSize
        };

        var sql = @"
            WITH root AS (SELECT 
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
            WHERE id = @parent_id AND is_active = true)

            SELECT root.*,
             (EXISTS(
                SELECT 1
                FROM departments
                WHERE parent_id = root.id
                AND is_active = true
                ORDER BY created_at 
                OFFSET @limit LIMIT 1)) AS HasMoreChildren
            FROM root

            UNION ALL

            -- Получаем дочерние подразделения
            SELECT
                children.id,
                children.name,
                children.identifier,
                children.ParentId,
                children.path,
                children.depth,
                children.IsActive,
                children.CreatedAt,
                children.UpdatedAt,
                (EXISTS( -- Для каждого из children добавляем флаг, есть ли у него дочерние подразделения (2)
                    SELECT 1 FROM departments
                    WHERE parent_id = children.id
                    AND is_active = true
                )) AS HasMoreChildren
            FROM root AS r
            CROSS JOIN LATERAL ( -- Получаем дочерние подразделения для данного родительского подразделения (1)
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
                WHERE d.parent_id = r.id AND d.is_active = true
                ORDER BY d.created_at
                OFFSET @offset LIMIT @limit
            ) AS children";

        var rawResults = await dbConnection.QueryAsync<DepartmentRawDto>(sql, parameters);

        var departments = rawResults.Select(r => new DepartmentDtoWithLazyLoadingOfChildren
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

        return Result.Success<DepartmentDtoWithLazyLoadingOfChildren[], Errors>(departments);
    }
}