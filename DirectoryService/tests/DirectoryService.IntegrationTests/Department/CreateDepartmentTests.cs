using DirectoryService.Application.Departments;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Locations.ValueObjects;
using DirectoryService.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Name = DirectoryService.Domain.Locations.ValueObjects.Name;

namespace DirectoryService.IntegrationTests.Department;

// Так как у нас жизненный цикл следующий:
// Конструктор DirectoryTestWebFactory
// Конструктор CreateDepartmentTests
// ПЕРВЫЙ тест
// Конструктор DirectoryTestWebFactory
// Конструктор CreateDepartmentTests
// ВТОРОЙ тест,
// а мы хотим, чтобы мы один раз запускали контейнер, то используем IClassFixture.
// Этот интерфейс нужен для объединения несколько классов в один общий контекст
public class CreateDepartmentTests : DirectoryBaseTests
{
    
    public CreateDepartmentTests(DirectoryTestWebFactory factory)
        : base(factory)
    {
    }

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
    public async void EmptyName()
    {
        // Arrange
        LocationId locationId = await CreateLocation();
        
        var cancellationToken = CancellationToken.None;
        
        // Act
        var result = await ExecuteHandler((sut) =>
        {
            var command = new CreateDepartmentCommand(new CreateDepartmentRequest(
                "",
                "podrazdelenie",
                null,
                [locationId.Value])); 
            return sut.Handle(command, cancellationToken);
        });
        
        // Assert
        await ExecuteInDb(async dbContext =>
        {
            Assert.NotEmpty(result.Error);
            Assert.True(result.IsFailure);
        });
    }
    
    [Fact]
    public async void NameLenghtLessThan3()
    {
        // Arrange
        LocationId locationId = await CreateLocation();
        
        var cancellationToken = CancellationToken.None;
        
        // Act
        var result = await ExecuteHandler((sut) =>
        {
            var command = new CreateDepartmentCommand(new CreateDepartmentRequest(
                "1",
                "podrazdelenie",
                null,
                [locationId.Value])); 
            return sut.Handle(command, cancellationToken);
        });
        
        // Assert
        await ExecuteInDb(async dbContext =>
        {
            Assert.NotEmpty(result.Error);
            Assert.True(result.IsFailure);
        });
    }
    
    [Fact]
    public async void EmptyIdentifier()
    {
        // Arrange
        LocationId locationId = await CreateLocation();
        
        var cancellationToken = CancellationToken.None;
        
        // Act
        var result = await ExecuteHandler((sut) =>
        {
            var command = new CreateDepartmentCommand(new CreateDepartmentRequest(
                "Подразделение",
                "",
                null,
                [locationId.Value])); 
            return sut.Handle(command, cancellationToken);
        });
        
        // Assert
        await ExecuteInDb(async dbContext =>
        {
            Assert.NotEmpty(result.Error);
            Assert.True(result.IsFailure);
        });
    }
    
    [Fact]
    public async void IdentifierLenghtLessThan3()
    {
        // Arrange
        LocationId locationId = await CreateLocation();
        
        var cancellationToken = CancellationToken.None;
        
        // Act
        var result = await ExecuteHandler((sut) =>
        {
            var command = new CreateDepartmentCommand(new CreateDepartmentRequest(
                "Подразделение",
                "    1   ",
                null,
                [locationId.Value])); 
            return sut.Handle(command, cancellationToken);
        });
        
        // Assert
        await ExecuteInDb(async dbContext =>
        {
            Assert.NotEmpty(result.Error);
            Assert.True(result.IsFailure);
        });
    }
    
    [Fact]
    public async void IdentifierWithNotLatinLetters()
    {
        // Arrange
        LocationId locationId = await CreateLocation();
        
        var cancellationToken = CancellationToken.None;
        
        // Act
        var result = await ExecuteHandler((sut) =>
        {
            var command = new CreateDepartmentCommand(new CreateDepartmentRequest(
                "Подразделение",
                "    Привет мир   ",
                null,
                [locationId.Value])); 
            return sut.Handle(command, cancellationToken);
        });
        
        // Assert
        await ExecuteInDb(async dbContext =>
        {
            Assert.NotEmpty(result.Error);
            Assert.True(result.IsFailure);
        });
    }
    
    [Fact]
    public async void InvalidLocationsIds()
    {
        // Arrange
        LocationId locationId = await CreateLocation();
        
        var cancellationToken = CancellationToken.None;
        
        // Act
        var result = await ExecuteHandler((sut) =>
        {
            var command = new CreateDepartmentCommand(new CreateDepartmentRequest(
                "Подразделение",
                "    hello world   ",
                null,
                [LocationId.NewLocationId()])); 
            return sut.Handle(command, cancellationToken);
        });
        
        // Assert
        await ExecuteInDb(async dbContext =>
        {
            Assert.NotEmpty(result.Error);
            Assert.True(result.IsFailure);
        });
    }
    
    [Fact]
    public async void RepeatedLocationsIds()
    {
        // Arrange
        LocationId locationId = await CreateLocation();
        
        var cancellationToken = CancellationToken.None;
        
        // Act
        var result = await ExecuteHandler((sut) =>
        {
            var command = new CreateDepartmentCommand(new CreateDepartmentRequest(
                "Подразделение",
                "    hello world   ",
                null,
                [locationId, locationId])); 
            return sut.Handle(command, cancellationToken);
        });
        
        // Assert
        await ExecuteInDb(async dbContext =>
        {
            Assert.NotEmpty(result.Error);
            Assert.True(result.IsFailure);
        });
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
}