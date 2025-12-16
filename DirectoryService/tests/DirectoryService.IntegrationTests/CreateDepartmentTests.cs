using DirectoryService.Application.Departments;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Locations.ValueObjects;
using DirectoryService.Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;
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
public class CreateDepartmentTests : IClassFixture<DirectoryTestWebFactory>, IAsyncLifetime
{
    private readonly Func<Task> _resetDatabase;
    
    public CreateDepartmentTests(DirectoryTestWebFactory factory)
    {
        Services = factory.Services;
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    public IServiceProvider Services { get; set; }

    [Fact]
    public async void CreateDepartmentWithValidData()
    {
        // Arrange
        LocationId locationId = await CreateLocation();
        
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await ExecuteHandler((sut) =>
        {
            var command = new CreateDepartmentCommand(new CreateDepartmentRequest(
                "Подразделение",
                "podrazdelenie",
                null,
                [locationId.Value])); 
            return sut.Handle(command, cancellationToken);
        });
        
        // Assert
        await ExecuteInDb(async dbContext =>
        {
            var department = await dbContext.Departments
                .FirstAsync(d => d.Id == DepartmentId.FromValue(result.Value), cancellationToken);
        
            Assert.NotNull(department);
            Assert.Equal(department.Id.Value, result.Value);
        
            Assert.True(result.IsSuccess);
            Assert.NotEqual(Guid.Empty, result.Value); 
        });
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
    
    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _resetDatabase(); 
    }

    private async Task<LocationId> CreateLocation()
    {
        return await ExecuteInDb(async dbContext =>
        {
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
        });
    }

    private async Task<T> ExecuteHandler<T>(Func<CreateDepartmentHandler, Task<T>> action)
    {
        var scope = Services.CreateAsyncScope();
        
        var sut = scope.ServiceProvider.GetRequiredService<CreateDepartmentHandler>();
        
        return await action(sut);
    }
    
    private async Task<T> ExecuteInDb<T>(Func<DirectoryServiceDbContext, Task<T>> action)
    {
        var scope = Services.CreateAsyncScope();
        
        var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        
        return await action(dbContext);
    }
    
    private async Task ExecuteInDb(Func<DirectoryServiceDbContext, Task> action)
    {
        var scope = Services.CreateAsyncScope();
        
        var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        
        await action(dbContext);
    }
}