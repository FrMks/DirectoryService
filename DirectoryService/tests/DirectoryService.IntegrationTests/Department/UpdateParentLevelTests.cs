using DirectoryService.Application.Departments;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Locations.ValueObjects;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Address = DirectoryService.Domain.Locations.ValueObjects.Address;
using Name = DirectoryService.Domain.Locations.ValueObjects.Name;
using Path = DirectoryService.Domain.Department.ValueObject.Path;

namespace DirectoryService.IntegrationTests.Department;

public class UpdateParentLevelTests : DirectoryBaseTests
{
    public UpdateParentLevelTests(DirectoryTestWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async void UpdateParentToParent()
    {
        // Arrange
        List<Guid> locationsIds = await CreateLocations(5);
        LocationId locationId = LocationId.FromValue(locationsIds[0]);
        // Создали родительский
        Domain.Department.Department departmentToMove = await CreateDepartment(locationId);
        
        var cancellationToken = CancellationToken.None;
        
        // Act
        var result = await ExecuteHandler(sut =>
        {
            var command = new UpdateParentLevelCommand(
                departmentToMove.Id,
                new UpdateParentLevelRequest(null));
            return sut.Handle(command, cancellationToken);
        });

        // Assert
        await ExecuteInDb(async dbContext =>
        {
            result.IsSuccess.Should().BeTrue();

            var department = dbContext.Departments
                .First(d => d.Id == departmentToMove.Id);

            department.Depth.Value.Should().Be(0);
            department.Path.Value.Should().Be("podrazdelenie");
        });
    }
    
    [Fact]
    public async void UpdateParentToChild()
    {
        // Arrange
        List<Guid> locationsIds = await CreateLocations(5);
        LocationId locationId = LocationId.FromValue(locationsIds[0]);
        Domain.Department.Department departmentToMove = await CreateDepartment(
            locationId,
            null,
            0,
            "department-to-move",
            "department-to-move");
        Domain.Department.Department whereDepartmentToMove = await CreateDepartment(
            locationId,
            null,
            0,
            "where-department-to-move",
            "department-to-move");
        
        var cancellationToken = CancellationToken.None;
        
        // Act
        var result = await ExecuteHandler(sut =>
        {
            var command = new UpdateParentLevelCommand(
                departmentToMove.Id,
                new UpdateParentLevelRequest(whereDepartmentToMove.Id));
            return sut.Handle(command, cancellationToken);
        });
    
        // Assert
        await ExecuteInDb(async dbContext =>
        {
            result.IsSuccess.Should().BeTrue();

            var childDepartment = dbContext.Departments
                .First(d => d.Id == departmentToMove.Id);
            var parentDepartment = dbContext.Departments
                .First(d => d.Id == whereDepartmentToMove.Id);

            childDepartment.Depth.Value.Should().Be(1);
            childDepartment.Path.Value.Should().Be("where-department-to-move.department-to-move");
            
            parentDepartment.Depth.Value.Should().Be(0);
            parentDepartment.Path.Value.Should().Be("where-department-to-move");
        });
    }
    
    [Fact]
    public async void UpdateChildToChild()
    {
        // Arrange
        List<Guid> locationsIds = await CreateLocations(5);
        LocationId locationId = LocationId.FromValue(locationsIds[0]);
        Domain.Department.Department firstParentDepartment = await CreateDepartment(
            locationId,
            null,
            0,
            "first-parent",
            "first-parent");
        Domain.Department.Department firstChildDepartmentFromFirstParent = await CreateDepartment(
            locationId,
            firstParentDepartment.Id,
            1,
            "first-parent.first-child",
            "first-child");
        
        Domain.Department.Department secondParentDepartment = await CreateDepartment(
            locationId,
            null,
            0,
            "second-parent",
            "second-parent");
        Domain.Department.Department firstChildDepartmentFromSecondParent = await CreateDepartment(
            locationId,
            firstParentDepartment.Id,
            1,
            "second-parent.first-child-from-second-parent",
            "first-child-from-second-parent");
        
        var cancellationToken = CancellationToken.None;
        
        // Act
        var result = await ExecuteHandler(sut =>
        {
            var command = new UpdateParentLevelCommand(
                firstChildDepartmentFromSecondParent.Id,
                new UpdateParentLevelRequest(firstChildDepartmentFromFirstParent.Id));
            return sut.Handle(command, cancellationToken);
        });
    
        // Assert
        await ExecuteInDb(async dbContext =>
        {
            result.IsSuccess.Should().BeTrue();

            var firstChildFromSecondParent = dbContext.Departments
                .First(d => d.Id == firstChildDepartmentFromSecondParent.Id);

            firstChildFromSecondParent.Depth.Value
                .Should()
                .Be(2);
            firstChildFromSecondParent.Path.Value
                .Should()
                .Be("first-parent.first-child.first-child-from-second-parent");
        });
    }
    
    [Fact]
    public async void UpdateChildToParent()
    {
        // Arrange
        List<Guid> locationsIds = await CreateLocations(5);
        LocationId locationId = LocationId.FromValue(locationsIds[0]);
        Domain.Department.Department parentDepartment = await CreateDepartment(
            locationId,
            null,
            0,
            "parent",
            "parent");
        Domain.Department.Department childDepartment = await CreateDepartment(
            locationId,
            parentDepartment.Id,
            1,
            "parent.child",
            "child");
        
        var cancellationToken = CancellationToken.None;
        
        // Act
        var result = await ExecuteHandler(sut =>
        {
            var command = new UpdateParentLevelCommand(
                childDepartment.Id,
                new UpdateParentLevelRequest(null));
            return sut.Handle(command, cancellationToken);
        });
    
        // Assert
        await ExecuteInDb(async dbContext =>
        {
            result.IsSuccess.Should().BeTrue();

            var newParent = dbContext.Departments
                .First(d => d.Id == childDepartment.Id);

            newParent.Depth.Value.Should().Be(0);
            newParent.Path.Value.Should().Be("child");
        });
    }
    
    private async Task<List<Guid>> CreateLocations(int countOfLocations)
    {
        var tempLocationsList = new List<Location>();
        
        return await ExecuteInDb(async dbContext =>
        {
            for (int i = 0; i < countOfLocations; i++)
            {
                LocationId locationId = LocationId.NewLocationId();
                
                var location = Location.Create(
                    locationId,
                    Name.Create($"Локация {i}").Value,
                    Address.Create($"Улица {i}", $"Город {i}", $"Страна {i}").Value,
                    Timezone.Create("Europe/London").Value,
                    new List<DepartmentLocation>()
                ).Value;
                
                tempLocationsList.Add(location);
            }
            
            dbContext.Locations.AddRange(tempLocationsList);
            await dbContext.SaveChangesAsync();
            
            var locationIdsResult = tempLocationsList.Select(l => l.Id.Value).ToList();
            
            return locationIdsResult;
        });
    }
    
    private async Task<Domain.Department.Department> CreateDepartment(
        LocationId locationId,
        Guid? parentId = null,
        short depth = 0,
        string path = "path",
        string identifier = "podrazdelenie")
    {
        return await ExecuteInDb(async dbContext =>
        {
            var departmentId = DepartmentId.NewDepartmentId();

            var departmentLocation = DepartmentLocation.Create(
                DepartmentLocationId.NewDepartmentId(),
                departmentId,
                locationId).Value;

            var department = Domain.Department.Department.Create(
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
    
    private async Task<T> ExecuteHandler<T>(Func<UpdateParentLevelHandler, Task<T>> action)
    {
        var scope = Services.CreateAsyncScope();
        
        var sut = scope.ServiceProvider.GetRequiredService<UpdateParentLevelHandler>();
        
        return await action(sut);
    }
}