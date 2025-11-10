using DirectoryService.Application.Locations;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICreateLocationHandler, CreateLocationHandler>();

        return services;
    }
}