using System.Data.Common;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Mvc.Testing;
using Respawn;
using Testcontainers.PostgreSql;
using Microsoft.Extensions.DependencyInjection;
using Amazon.S3;
using Microsoft.Extensions.Options;
using FileService.Infrastructure.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FileService.Infrastructure.Postgres;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FileService.IntegrationTests.Infrastructure;

public class FileServiceTestWebFactory : WebApplicationFactory<FileService.Web.Program>, IAsyncLifetime
{
    // Запускаем один Docker контейнер с нашей БД для тестов и MinIO
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("file_service_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    /// <summary>
    /// Создает builder, через который описывается будущий Docker-контейнер.
    /// ---------
    /// WithImage говорит, какой Docker image использовать.
    /// ---------
    /// WithPortBinding(9000, true) - пробрасывает порт контейнера наружу. 
    /// (MinIO внутри контейнера слушает порт 9000. Но тестовый код работает снаружи контейнера,
    /// на моей машине. Поэтому нужно сделать этот порт доступным с хоста.)
    /// 9000 - внутренний порт контейнера. true - выбери случайный свободный порт на хосте. 
    /// Например container:9000 -> localhost:51423.
    /// ---------
    /// WithEnvironment можно вызывать сколько угодно раз. Один вызов - одна переменная окружения.
    /// ---------
    /// WithCommand("server", "/data") - задает команду, с которой запускается контейнер.
    /// Для MinIO это аналог команды в терминале: minio server /data
    /// server - означает запусти MinIO в режиме object storage server
    /// /data - означает храни данные внутри контейнера в директории /data
    /// ---------
    /// WithWaitStrategy.. Говорит TestContainers:
    /// "После запуска контейнера не считай его готовым сразу. Дождись пока внутри контейнера станет доступен TCP-порт 9000"
    /// </summary>
    private readonly DotNet.Testcontainers.Containers.IContainer _minioContainer = new ContainerBuilder()
        .WithImage("minio/minio:latest")
        .WithPortBinding(9000, true) // Порт 9000 внутри контейнера пробросить на случайный свободный порт хоста
        .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
        .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
        .WithCommand("server", "/data")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(9000))
        .Build();

    private Respawner _respawner = null!;
    private DbConnection _dbConnection = null!;

    // Т.к. у нас указывается подключение к БД через connection string внутри Program.cs, нам нужно подменить
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Удаляем зарегистрированные в DI в Program.cs 
            services.RemoveAll<FileServiceDbContext>();

            // И подменяем
            services.AddScoped<FileServiceDbContext>(_ =>
                new FileServiceDbContext(_dbContainer.GetConnectionString()));
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            string minioEndpoint = $"http://localhost:{_minioContainer.GetMappedPublicPort(9000)}";

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["S3Options:Endpoint"] = minioEndpoint,
                ["S3Options:AccessKey"] = "minioadmin",
                ["S3Options:SecretKey"] = "minioadmin",
                ["S3Options:WithSsl"] = "false",
                ["S3Options:RequiredBuckets:0"] = "videos",
                ["S3Options:RequiredBuckets:1"] = "preview",
                ["S3Options:UploadUrlExpirationMinutes"] = "15",
                ["S3Options:DownloadUrlExpirationHours"] = "24",
                ["S3Options:MaxConcurrentRequests"] = "20",
                ["S3Options:RecommendedChunkSizeBytes"] = "5242880",
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _minioContainer.StartAsync();

        await using var scope = Services.CreateAsyncScope();
        FileServiceDbContext dbContext = scope.ServiceProvider.GetRequiredService<FileServiceDbContext>();

        // Создаем миграции, чтобы тесты могли работать с тестовой БД развернутой в контейнере
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        IS3BucketInitializer bucketInitializer = scope.ServiceProvider.GetRequiredService<IS3BucketInitializer>();
        await bucketInitializer.InitializeAsync();

        _dbConnection = new NpgsqlConnection(_dbContainer.GetConnectionString());
        await InitializeRespawner();
    }

    // Добавили new, чтобы сказать, чтобы именно этот вызывался для dispose этого класса
    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _minioContainer.StopAsync();
        await _dbContainer.DisposeAsync();
        await _minioContainer.DisposeAsync();

        await _dbConnection.CloseAsync();
        await _dbConnection.DisposeAsync();
    }

    public async Task ResetStorageAsync()
    {
        await using var scope = Services.CreateAsyncScope();

        IAmazonS3 s3Client = scope.ServiceProvider.GetRequiredService<IAmazonS3>();
        S3Options options = scope.ServiceProvider.GetRequiredService<IOptions<S3Options>>().Value;

        foreach (var bucket in options.RequiredBuckets)
        {
            // Дай мне объекты бакета
            var listRequest = new ListObjectsV2Request
            {
                BucketName = bucket,
            };

            ListObjectsV2Response listResponse;

            do
            {
                // Список найденных объектов, у каждого есть Key
                listResponse = await s3Client.ListObjectsV2Async(listRequest);
                IReadOnlyList<S3Object> objects = listResponse.S3Objects ?? [];

                if (objects.Count > 0)
                {
                    var deleteRequest = new DeleteObjectsRequest
                    {
                        BucketName = bucket,
                        Objects = listResponse.S3Objects
                            // KeyVersion - объект, который говорит S3 удали object с таким key
                            .Select(obj => new KeyVersion { Key = obj.Key })
                            .ToList(),
                    };

                    await s3Client.DeleteObjectsAsync(deleteRequest);
                }

                // NextContinuationToken говорит, как запросить следующую страницу.
                listRequest.ContinuationToken = listResponse.NextContinuationToken;
            }
            while (listResponse.IsTruncated == true); // если объектов много
        }
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }

    private async Task InitializeRespawner()
    {
        await _dbConnection.OpenAsync();
        _respawner = await Respawner.CreateAsync(
            _dbConnection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
            });
    }
}
