using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Program = DirectoryService.Presentation.Program;

namespace DirectoryService.IntegrationTests;

public class DirectoryTestWebFactory : WebApplicationFactory<Program>
{
    // Запускаем один Docker контейнер с нашей БД для тестов
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres")
        .WithDatabase("directory_service_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public DirectoryTestWebFactory()
    {
        // _dbContainer.StartAsync()
    }
}