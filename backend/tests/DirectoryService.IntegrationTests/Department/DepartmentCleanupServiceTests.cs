using DirectoryService.Domain;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.DepartmentPositions.ValueObjects;
using DirectoryService.Domain.Locations.ValueObjects;
using DirectoryService.Domain.Positions;
using DirectoryService.Domain.Positions.ValueObject;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.IntegrationTests.Infrastructure;
using DirectoryService.Infrastructure.Postgres.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Address = DirectoryService.Domain.Locations.ValueObjects.Address;
using DepartmentEntity = DirectoryService.Domain.Department.Department;
using Location = DirectoryService.Domain.Locations.Location;
using LocationName = DirectoryService.Domain.Locations.ValueObjects.Name;
using Path = DirectoryService.Domain.Department.ValueObject.Path;
using PositionDescription = DirectoryService.Domain.Positions.ValueObject.Description;
using PositionName = DirectoryService.Domain.Positions.ValueObject.Name;

namespace DirectoryService.IntegrationTests.Department;

public class DepartmentCleanupServiceTests : DirectoryBaseTests
{
    public DepartmentCleanupServiceTests(DirectoryTestWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async void CleanupInactiveDepartmentsDeleteOldInactiveDepartment()
    {
        // Arrange
        Guid locationId = await CreateLocation("main-location");
        Guid positionId = await CreatePosition("main-position");

        DepartmentEntity deletedDepartment = await CreateDepartmentWithRelations(
            locationId,
            positionId,
            null,
            0,
            "deleted-it",
            "deleted-it");

        await MakeDepartmentInactive(deletedDepartment.Id.Value, 31);

        var cancellationToken = CancellationToken.None;

        // Act
        await ExecuteService(sut => sut.CleanupInactiveDepartments(30, cancellationToken));

        // Assert
        await ExecuteInDb(async dbContext =>
        {
            var deletedDepartmentFromDb = await dbContext.Departments
                .FirstOrDefaultAsync(d => d.Id == deletedDepartment.Id, cancellationToken);

            var departmentLocationsCount = await dbContext.DepartmentLocations
                .CountAsync(dl => dl.DepartmentId == deletedDepartment.Id, cancellationToken);

            var departmentPositionsCount = await dbContext.DepartmentPositions
                .CountAsync(dp => dp.DepartmentId == deletedDepartment.Id, cancellationToken);

            deletedDepartmentFromDb.Should().BeNull();
            departmentLocationsCount.Should().Be(0);
            departmentPositionsCount.Should().Be(0);
        });
    }

    [Fact]
    public async void CleanupInactiveDepartmentsUpdateChildrenPathAndParentId()
    {
        // Arrange
        Guid locationId = await CreateLocation("tree-location");

        DepartmentEntity rootDepartment = await CreateDepartment(
            locationId,
            null,
            0,
            "hq",
            "headquarter");

        DepartmentEntity deletedDepartment = await CreateDepartment(
            locationId,
            rootDepartment.Id.Value,
            1,
            "hq.deleted-it",
            "deleted-it");

        DepartmentEntity childDepartment = await CreateDepartment(
            locationId,
            deletedDepartment.Id.Value,
            2,
            "hq.deleted-it.dev-team",
            "dev-team");

        DepartmentEntity grandChildDepartment = await CreateDepartment(
            locationId,
            childDepartment.Id.Value,
            3,
            "hq.deleted-it.dev-team.backend",
            "backend");

        await MakeDepartmentInactive(deletedDepartment.Id.Value, 31);

        var cancellationToken = CancellationToken.None;

        // Act
        await ExecuteService(sut => sut.CleanupInactiveDepartments(30, cancellationToken));

        // Assert
        await ExecuteInDb(async dbContext =>
        {
            var deletedDepartmentFromDb = await dbContext.Departments
                .FirstOrDefaultAsync(d => d.Id == deletedDepartment.Id, cancellationToken);

            var childDepartmentFromDb = await dbContext.Departments
                .FirstAsync(d => d.Id == childDepartment.Id, cancellationToken);

            var grandChildDepartmentFromDb = await dbContext.Departments
                .FirstAsync(d => d.Id == grandChildDepartment.Id, cancellationToken);

            deletedDepartmentFromDb.Should().BeNull();

            childDepartmentFromDb.ParentId.Should().Be(rootDepartment.Id.Value);
            childDepartmentFromDb.Path.Value.Should().Be("hq.dev-team");
            childDepartmentFromDb.Depth.Value.Should().Be(1);

            grandChildDepartmentFromDb.ParentId.Should().Be(childDepartment.Id.Value);
            grandChildDepartmentFromDb.Path.Value.Should().Be("hq.dev-team.backend");
            grandChildDepartmentFromDb.Depth.Value.Should().Be(2);
        });
    }

    [Fact]
    public async void CleanupInactiveDepartmentsDoNotDeleteNewInactiveDepartment()
    {
        // Arrange
        Guid locationId = await CreateLocation("fresh-location");

        DepartmentEntity deletedDepartment = await CreateDepartment(
            locationId,
            null,
            0,
            "new-deleted-department",
            "new-deleted-department");

        await MakeDepartmentInactive(deletedDepartment.Id.Value, 5);

        var cancellationToken = CancellationToken.None;

        // Act
        await ExecuteService(sut => sut.CleanupInactiveDepartments(30, cancellationToken));

        // Assert
        await ExecuteInDb(async dbContext =>
        {
            var departmentFromDb = await dbContext.Departments
                .FirstOrDefaultAsync(d => d.Id == deletedDepartment.Id, cancellationToken);

            departmentFromDb.Should().NotBeNull();
            departmentFromDb!.IsActive.Should().BeFalse();
        });
    }

    private async Task<Guid> CreateLocation(string name)
    {
        return await ExecuteInDb(async dbContext =>
        {
            var location = Location.Create(
                LocationId.NewLocationId(),
                LocationName.Create(name).Value,
                Address.Create("Street", "City", "Country").Value,
                Timezone.Create("Europe/London").Value,
                new List<DepartmentLocation>()).Value;

            dbContext.Locations.Add(location);
            await dbContext.SaveChangesAsync();

            return location.Id.Value;
        });
    }

    private async Task<Guid> CreatePosition(string name)
    {
        return await ExecuteInDb(async dbContext =>
        {
            var position = Position.Create(
                PositionId.NewPositionId(),
                PositionName.Create(name).Value,
                PositionDescription.Create("Position description").Value,
                []).Value;

            dbContext.Positions.Add(position);
            await dbContext.SaveChangesAsync();

            return position.Id.Value;
        });
    }

    private async Task<DepartmentEntity> CreateDepartment(
        Guid locationId,
        Guid? parentId = null,
        short depth = 0,
        string path = "path",
        string identifier = "department")
    {
        return await ExecuteInDb(async dbContext =>
        {
            var departmentId = DepartmentId.NewDepartmentId();

            var departmentLocation = DepartmentLocation.Create(
                DepartmentLocationId.NewDepartmentId(),
                departmentId,
                LocationId.FromValue(locationId)).Value;

            var department = DepartmentEntity.Create(
                departmentId,
                Domain.Department.ValueObject.Name.Create("Подразделение").Value,
                Identifier.Create(identifier).Value,
                Path.Create(path).Value,
                [departmentLocation],
                Depth.Create(depth).Value,
                parentId).Value;

            dbContext.Departments.Add(department);
            await dbContext.SaveChangesAsync();

            return department;
        });
    }

    private async Task<DepartmentEntity> CreateDepartmentWithRelations(
        Guid locationId,
        Guid positionId,
        Guid? parentId = null,
        short depth = 0,
        string path = "path",
        string identifier = "department")
    {
        return await ExecuteInDb(async dbContext =>
        {
            var departmentId = DepartmentId.NewDepartmentId();

            var departmentLocation = DepartmentLocation.Create(
                DepartmentLocationId.NewDepartmentId(),
                departmentId,
                LocationId.FromValue(locationId)).Value;

            var departmentPosition = DepartmentPosition.Create(
                DepartmentPositionId.NewDepartmentId(),
                departmentId,
                PositionId.FromValue(positionId)).Value;

            var department = DepartmentEntity.Create(
                departmentId,
                Domain.Department.ValueObject.Name.Create("Подразделение").Value,
                Identifier.Create(identifier).Value,
                Path.Create(path).Value,
                [departmentLocation],
                [departmentPosition],
                Depth.Create(depth).Value,
                parentId).Value;

            dbContext.Departments.Add(department);
            await dbContext.SaveChangesAsync();

            return department;
        });
    }

    private async Task MakeDepartmentInactive(Guid departmentId, int deletedDaysAgo)
    {
        await ExecuteInDb(async dbContext =>
        {
            var department = await dbContext.Departments
                .FirstAsync(d => d.Id == DepartmentId.FromValue(departmentId));

            department.SoftDelete();
            await dbContext.SaveChangesAsync();

            var deletedAt = DateTime.UtcNow.AddDays(-deletedDaysAgo);

            await dbContext.Database.ExecuteSqlAsync(
                $"""
                UPDATE departments
                SET deleted_at = {deletedAt}
                WHERE id = {departmentId}
                """);
        });
    }

    private async Task ExecuteService(Func<DepartmentCleanupService, Task> action)
    {
        var scope = Services.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<DepartmentCleanupService>();
        await action(sut);
    }
}
