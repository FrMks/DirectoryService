using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Application.Abstractions;
using DirectoryService.Contracts.Departments.GetTopDepartments;
using Shared;
using Shared.Database;

namespace DirectoryService.Application.Departments.GetTopDepartments;

public class GetTopDepartmentsDapperHandler(
    IDbConnectionFactory dbConnectionFactory) : IQueryHandler<Result<DepartmentWithPositionsDapperDto[], Errors>>
{
    public async Task<Result<DepartmentWithPositionsDapperDto[], Errors>> Handle(CancellationToken cancellationToken)
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

        return Result.Success<DepartmentWithPositionsDapperDto[], Errors>(queryResult.ToArray());
    }
}