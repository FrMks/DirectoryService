using DirectoryService.Infrastructure.Postgres;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Program = DirectoryService.Presentation.Program;

namespace DirectoryService.IntegrationTests;

public class DirectoryTestWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Запускаем один Docker контейнер с нашей БД для тестов
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres")
        .WithDatabase("directory_service_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();
    
    // Т.к. у нас указывается подключение к БД через connection string внутри Program.cs, нам нужно подменить
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Удаляем зарегистрированные в DI в Program.cs 
            services.RemoveAll<DirectoryServiceDbContext>();
            
            // И подменяем
            services.AddScoped<DirectoryServiceDbContext>(_ =>
                new DirectoryServiceDbContext(_dbContainer.GetConnectionString()));
        });
    }

    // Реализую методы из интерфейса IAsyncLifetime,
    // чтобы асинхронно выполнять какие-то методы для инициализации класса
    // и для его уничтожения
    // ЭТО НУЖНО ЧТОБЫ НЕ ВЫЗЫВАТЬ АСИНХРОННЫЕ МЕТОДЫ ВНУТРИ КОНСТРУКТОРА
    // Т.К. В КОНСТРУКТОРЕ НЕЛЬЗЯ ВЫЗЫВАТЬ АСИНХРОННЫЕ МЕТОДЫ
    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        
        // Создаем миграции, чтобы тесты могли работать с тестовой БД развернутой в контейнере
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    // Добавили new, чтобы сказать, чтобы именно этот вызывался для dispose этого класса
    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }
}