using FileService.Core.Event;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileService.Infrastructure.RabbitMQ;

public static class DependencyInjection
{
    public static IServiceCollection AddRabbitMQ(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IEventPublisher, EventPublisher>();

        return services;
    }
}