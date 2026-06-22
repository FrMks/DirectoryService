using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileService.Communication;

// Чтобы указать: services.AddFilesService(configuration); в другом микросервисе.
public static class FileServiceExtensions
{
    public static IServiceCollection AddFilesServiceHttpCommunication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FileServiceOptions>(configuration.GetSection(nameof(FileServiceOptions)));

        services.AddHttpClient<IFileCommunicationService, FileHttpClient>((serviceProvider, clientConfig) =>
        {
            FileServiceOptions fileOptions = serviceProvider.GetRequiredService<IOptions<FileServiceOptions>>().Value;

            clientConfig.BaseAddress = new Uri(fileOptions.Url);
            clientConfig.Timeout = TimeSpan.FromSeconds(fileOptions.TimeoutSeconds);
        });

        return services;
    }
}