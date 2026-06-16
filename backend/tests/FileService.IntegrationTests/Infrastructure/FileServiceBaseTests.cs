using FileService.Core.Files;
using FileService.Infrastructure.Postgres;
using Microsoft.Extensions.DependencyInjection;

namespace FileService.IntegrationTests.Infrastructure;

// IClassFixture<FileServiceTestWebFactory> создает один экземпляр FileServiceTestWebFactory и передает его в конструктор тестового класса
// xUnit сам создаст factory и передаст его в конструктор
// Это нужно для: не стартовать Postgres/MinIO вручную в каждом тесте
// иметь один общий WebApplicationFacoty
// иметь доступ к factory.Services
// переиспользовать контейнеры внутри одного test class
public class FileServiceBaseTests : IClassFixture<FileServiceTestWebFactory>, IAsyncLifetime
{
    private readonly Func<Task> _resetDatabase;
    private readonly Func<Task> _resetStorage;

    protected FileServiceBaseTests(FileServiceTestWebFactory factory)
    {
        Services = factory.Services;
        _resetDatabase = factory.ResetDatabaseAsync;
        _resetStorage = factory.ResetStorageAsync;
    }

    protected IServiceProvider Services { get; set; }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _resetDatabase();
        await _resetStorage();
    }

    protected async Task<T> ExecuteInDb<T>(Func<FileServiceDbContext, Task<T>> action)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();

        FileServiceDbContext dbContext = scope.ServiceProvider.GetRequiredService<FileServiceDbContext>();

        return await action(dbContext);
    }

    protected async Task ExecuteInDb(Func<FileServiceDbContext, Task> action)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();

        FileServiceDbContext dbContext = scope.ServiceProvider.GetRequiredService<FileServiceDbContext>();

        await action(dbContext);
    }

    protected async Task<T> ExecuteWithStorage<T>(Func<IS3Provider, Task<T>> action)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();

        IS3Provider storage = scope.ServiceProvider.GetRequiredService<IS3Provider>();

        return await action(storage);
    }

    protected async Task ExecuteWithStorage(Func<IS3Provider, Task> action)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();

        IS3Provider storage = scope.ServiceProvider.GetRequiredService<IS3Provider>();

        await action(storage);
    }
}