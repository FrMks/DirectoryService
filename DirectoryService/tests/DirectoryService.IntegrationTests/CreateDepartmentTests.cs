using DirectoryService.Application.Departments;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Locations.ValueObjects;
using DirectoryService.Infrastructure.Postgres;
using Microsoft.Extensions.DependencyInjection;
using Name = DirectoryService.Domain.Locations.ValueObjects.Name;

namespace DirectoryService.IntegrationTests;

// Так как у нас жизненный цикл следующий:
// Конструктор DirectoryTestWebFactory
// Конструктор CreateDepartmentTests
// ПЕРВЫЙ тест
// Конструктор DirectoryTestWebFactory
// Конструктор CreateDepartmentTests
// ВТОРОЙ тест,
// а мы хотим, чтобы мы один раз запускали контейнер, то используем IClassFixture.
// Этот интерфейс нужен для объединения несколько классов в один общий контекст
public class CreateDepartmentTests : IClassFixture<DirectoryTestWebFactory>
{
    public CreateDepartmentTests(DirectoryTestWebFactory factory)
    {
        Services = factory.Services;
    }

    public IServiceProvider Services { get; set; }

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