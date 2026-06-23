using WireMock.Server;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using FileService.Communication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using Shared;
using FluentAssertions;

namespace DirectoryService.IntegrationTests.FileServiceCommunication;

// Фальшивый File Service
// ^ Http
// |
// IFileCommunicationService
// ^ DI
// |
// DirectoryService.IntegrationTests
public class FileServiceCommunicationTests
{
    [Fact]
    public async Task GetContentUrlByFileIdFromDirectoryService()
    {
        Guid fileId = Guid.NewGuid();

        WireMockServer server = WireMockServer.Start();

        server
            .Given(Request.Create().WithPath($"/files/{fileId:D}/content-url").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "result": {
                        "url": "https://example.com/file.jpg"
                    },
                    "errorList": []
                }
                """));

        // Создаем аналог appsettings.json только прям в памяти
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{FileServiceOptions.SectionName}:Url"] = server.Url!, // URL fake-сервера появляется динамически. Заранее в appsettings.json такой порт не знаешь
                [$"{FileServiceOptions.SectionName}:TimeoutSeconds"] = "7",
                [$"{FileServiceOptions.SectionName}:AttemptTimeoutSeconds"] = "2",
                [$"{FileServiceOptions.SectionName}:RetryCount"] = "2",
                [$"{FileServiceOptions.SectionName}:RetryDelayMilliseconds"] = "200",
                [$"{FileServiceOptions.SectionName}:CircuitBreakerFailureRatio"] = "0.5",
                [$"{FileServiceOptions.SectionName}:CircuitBreakerMinimumThroughput"] = "5",
                [$"{FileServiceOptions.SectionName}:CircuitBreakerSamplingSeconds"] = "10",
                [$"{FileServiceOptions.SectionName}:CircuitBreakerBreakSeconds"] = "15",
            })
            .Build();
        ServiceCollection services = new();
        // Создаем пустой DI контейнер
        services.AddFilesServiceHttpCommunication(configuration);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IFileCommunicationService fileClient = serviceProvider
            .GetRequiredService<IFileCommunicationService>();

        Result<GetContentUrlResponse, Errors> contentUrlResponseResult = await fileClient
            .GetContentUrlAsync(fileId, CancellationToken.None);
        if (contentUrlResponseResult.IsFailure)
        {
            contentUrlResponseResult.IsSuccess.Should().BeTrue();
        }

        GetContentUrlResponse contentUrlResponse = contentUrlResponseResult.Value;
        contentUrlResponse.Url.Should().Be("https://example.com/file.jpg");
    }

    [Fact]
    public async Task ContentUrlNotFound()
    {
        Guid fileId = Guid.NewGuid();

        using WireMockServer server = WireMockServer.Start();

        server
            .Given(Request.Create().WithPath($"/files/{fileId:D}/content-url").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "result": null,
                    "errorList": [
                        {
                            "code": "file.not_found",
                            "message": "File not found",
                            "type": 1,
                            "invalidField": null
                        }
                    ]
                }
                """));

        // Создаем аналог appsettings.json только прям в памяти
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{FileServiceOptions.SectionName}:Url"] = server.Url!, // URL fake-сервера появляется динамически. Заранее в appsettings.json такой порт не знаешь
                [$"{FileServiceOptions.SectionName}:TimeoutSeconds"] = "7",
                [$"{FileServiceOptions.SectionName}:AttemptTimeoutSeconds"] = "2",
                [$"{FileServiceOptions.SectionName}:RetryCount"] = "2",
                [$"{FileServiceOptions.SectionName}:RetryDelayMilliseconds"] = "200",
                [$"{FileServiceOptions.SectionName}:CircuitBreakerFailureRatio"] = "0.5",
                [$"{FileServiceOptions.SectionName}:CircuitBreakerMinimumThroughput"] = "5",
                [$"{FileServiceOptions.SectionName}:CircuitBreakerSamplingSeconds"] = "10",
                [$"{FileServiceOptions.SectionName}:CircuitBreakerBreakSeconds"] = "15",
            })
            .Build();
        ServiceCollection services = new();
        // Создаем пустой DI контейнер
        services.AddFilesServiceHttpCommunication(configuration);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IFileCommunicationService fileClient = serviceProvider
            .GetRequiredService<IFileCommunicationService>();

        Result<GetContentUrlResponse, Errors> contentUrlResponseResult = await fileClient
            .GetContentUrlAsync(fileId, CancellationToken.None);
        if (contentUrlResponseResult.IsFailure)
        {
            contentUrlResponseResult.IsFailure.Should().BeTrue();
            contentUrlResponseResult.Error.Should().Contain(error => error.Code == "file.not_found");
            server.LogEntries.Count().Should().Be(1);
        }
    }
}