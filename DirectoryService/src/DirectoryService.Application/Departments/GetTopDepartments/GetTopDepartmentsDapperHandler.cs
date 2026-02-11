using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Application.Abstractions;
using DirectoryService.Contracts.Departments.GetTopDepartments;
using DirectoryService.Domain.Department;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.DepartmentLocations;
using Shared;
using Shared.Database;
using DepartmentPath = DirectoryService.Domain.Department.ValueObject.Path;

namespace DirectoryService.Application.Departments.GetTopDepartments;

public class GetTopDepartmentsDapperHandler(
    IDbConnectionFactory dbConnectionFactory) : IQueryHandler<Result<TopDepartmentsDapperResponse, Errors>>
{
    public async Task<Result<TopDepartmentsDapperResponse, Errors>> Handle(CancellationToken cancellationToken)
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
                COUNT(dp.id) as PositionsCount
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

        var queryResult = await dbConnection.QueryAsync<DepartmentDapperDto>(sql);

        var topDepartments = queryResult
            .Select(dto => new DepartmentWithPositionsDto(
                Department: MapToDepartment(dto),
                PositionsCount: dto.PositionsCount))
            .ToList();

        var response = new TopDepartmentsDapperResponse(topDepartments);

        return response;
    }

    private Department MapToDepartment(DepartmentDapperDto dto)
    {
        return Department.Create(
            DepartmentId.FromValue(dto.Id),
            Name.Create(dto.Name).Value,
            Identifier.Create(dto.Identifier).Value,
            DepartmentPath.Create(dto.Path).Value,
            Enumerable.Empty<DepartmentLocation>(),
            Depth.Create(dto.Depth).Value,
            dto.ParentId).Value;
    }
}