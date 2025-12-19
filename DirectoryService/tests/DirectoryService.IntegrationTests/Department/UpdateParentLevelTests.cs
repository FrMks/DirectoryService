using DirectoryService.Application.Departments;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Locations.ValueObjects;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.IntegrationTests.Infrastructure;
using Docker.DotNet.Models;
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
    
    // [Fact]
    // public async void UpdateParentToChild()
    // {
    //     // Arrange
    //     List<Guid> locationsIds = await CreateLocations(5);
    //     LocationId locationId = LocationId.FromValue(locationsIds[0]);
    //     // Создали родительский
    //     Domain.Department.Department department = await CreateDepartment(locationId);
    //     
    //     var cancellationToken = CancellationToken.None;
    //     
    //     // Act
    //     var result = await ExecuteHandler(sut =>
    //     {
    //         var command = new UpdateParentLevelCommand(
    //             departmentToMove.Id,
    //             new UpdateParentLevelRequest(whereDepartmentToMove.Id));
    //         return sut.Handle(command, cancellationToken);
    //     });
    //
    //     // Assert
    //
    // }
    //
    // [Fact]
    // public async void UpdateChildToChild()
    // {
    //     // Arrange
    //     List<Guid> locationsIds = await CreateLocations(5);
    //     LocationId locationId = LocationId.FromValue(locationsIds[0]);
    //     // Создали родительский
    //     Domain.Department.Department department = await CreateDepartment(locationId);
    //     
    //     var cancellationToken = CancellationToken.None;
    //     
    //     // Act
    //     var result = await ExecuteHandler(sut =>
    //     {
    //         var command = new UpdateParentLevelCommand(
    //             departmentToMove.Id,
    //             new UpdateParentLevelRequest(whereDepartmentToMove.Id));
    //         return sut.Handle(command, cancellationToken);
    //     });
    //
    //     // Assert
    //
    // }
    //
    // [Fact]
    // public async void UpdateChildToParent()
    // {
    //     // Arrange
    //     List<Guid> locationsIds = await CreateLocations(5);
    //     LocationId locationId = LocationId.FromValue(locationsIds[0]);
    //     // Создали родительский
    //     Domain.Department.Department department = await CreateDepartment(locationId);
    //     
    //     var cancellationToken = CancellationToken.None;
    //     
    //     // Act
    //     var result = await ExecuteHandler(sut =>
    //     {
    //         var command = new UpdateParentLevelCommand(
    //             departmentToMove.Id,
    //             new UpdateParentLevelRequest(whereDepartmentToMove.Id));
    //         return sut.Handle(command, cancellationToken);
    //     });
    //
    //     // Assert
    //
    // }
    
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
        string path = "path")
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
                Identifier.Create("podrazdelenie").Value,
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