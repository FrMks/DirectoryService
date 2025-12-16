using DirectoryService.Application.Departments;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Locations.ValueObjects;
using DirectoryService.Infrastructure.Postgres;
using Microsoft.Extensions.DependencyInjection;
using Name = DirectoryService.Domain.Locations.ValueObjects.Name;

namespace DirectoryService.IntegrationTests;

public class CreateDepartmentTests : DirectoryTestWebFactory
{
    [Fact]
    public async void CreateDepartmentWithValidData()
    {
        // Тут хранятся все зарегистрированный в DI сервисы
        // var services = Services;

        // Arrange
        LocationId locationId = await CreateLocation();
        
        await using var scope = Services.CreateAsyncScope();

        var sut = scope.ServiceProvider.GetRequiredService<CreateDepartmentHandler>();
        var cancellationToken = CancellationToken.None;
        var command = new CreateDepartmentCommand(new CreateDepartmentRequest(
            "Подразделение",
            "podrazdelenie",
            null,
            [locationId.Value]));

        // Act
        var result = await sut.Handle(command, cancellationToken);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        
    }

    [Fact]
    public async void CreateDepartWithUnvalidName()
    {
        
    }
    
    [Fact]
    public async void CreateDepartWithUnvalidIdentifier()
    {
        
    }
    
    [Fact]
    public async void CreateDepartWithUnvalidLocationsIds()
    {
        
    }

    private async Task<LocationId> CreateLocation()
    {
        await using var initializerScope = Services.CreateAsyncScope();
        var dbContext = initializerScope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();

        LocationId locationId = LocationId.NewLocationId();
            
        var location = Location.Create(
            locationId, 
            Name.Create("Локация").Value,
            Address.Create("Улица", "Город", "Страна").Value,
            Timezone.Create("Europe/London").Value,
            new List<DepartmentLocation>()
        ).Value;
            
        dbContext.Locations.Add(location);
        await dbContext.SaveChangesAsync();

        return locationId;
    }
}