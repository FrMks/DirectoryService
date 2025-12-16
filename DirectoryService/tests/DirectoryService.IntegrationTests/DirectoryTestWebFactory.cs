using Microsoft.AspNetCore.Mvc.Testing;
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

    // Реализую методы из интерфейса IAsyncLifetime,
    // чтобы асинхронно выполнять какие-то методы для инициализации класса
    // и для его уничтожения
    // ЭТО НУЖНО ЧТОБЫ НЕ ВЫЗЫВАТЬ АСИНХРОННЫЕ МЕТОДЫ ВНУТРИ КОНСТРУКТОРА
    // Т.К. В КОНСТРУКТОРЕ НЕЛЬЗЯ ВЫЗЫВАТЬ АСИНХРОННЫЕ МЕТОДЫ
    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    // Добавили new, чтобы сказать, чтобы именно этот вызывался для dispose этого класса
    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }
}