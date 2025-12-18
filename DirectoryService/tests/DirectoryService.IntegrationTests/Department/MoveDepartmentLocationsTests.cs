using DirectoryService.Application.Departments;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Locations.ValueObjects;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Name = DirectoryService.Domain.Locations.ValueObjects.Name;
using Path = DirectoryService.Domain.Department.ValueObject.Path;

namespace DirectoryService.IntegrationTests.Department;

public class MoveDepartmentLocationsTests : DirectoryBaseTests
{
    public MoveDepartmentLocationsTests(DirectoryTestWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async void UpdateDepartmentLocationsWithValidData()
    {
        // Arrange
        List<Guid> locationsIds = await CreateLocations(5);
        LocationId locationId = LocationId.FromValue(locationsIds[0]);
        Domain.Department.Department department = await CreateDepartment(locationId);
        
        var cancellationToken = CancellationToken.None;
        
        // Act
        var result = await ExecuteHandler(sut =>
        {
            var command = new UpdateDepartmentLocationsCommand(
                department.Id,
                new UpdateDepartmentLocationsRequest(locationsIds));
            return sut.Handle(command, cancellationToken);
        });

        // Assert
        await ExecuteInDb(async dbContext =>
        {
            var departmentLocations = await dbContext.DepartmentLocations
                .Where(dl => dl.DepartmentId == department.Id)
                .ToListAsync(cancellationToken);
            
            departmentLocations.Should().NotBeEmpty();
            result.IsSuccess.Should().BeTrue();
            // TODO: Вопрос. Я изначально поставил 5, потому что думал, что у меня полностью обновляется и становится 5
            // в var updateResult = department.UpdateDepartmentLocations(departmentLocations);
            departmentLocations.Count.Should().Be(6);
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

    private async Task<Domain.Department.Department> CreateDepartment(LocationId locationId)
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
                Path.Create("path").Value,
                [departmentLocation],
                Depth.Create(0).Value,
                null).Value;
            
            dbContext.Departments.Add(department);
            await dbContext.SaveChangesAsync();

            return department;
        });
    }

    private async Task<T> ExecuteHandler<T>(Func<UpdateDepartmentLocationsHandler, Task<T>> action)
    {
        var scope = Services.CreateAsyncScope();
        
        var sut = scope.ServiceProvider.GetRequiredService<UpdateDepartmentLocationsHandler>();
        
        return await action(sut);
    }
}