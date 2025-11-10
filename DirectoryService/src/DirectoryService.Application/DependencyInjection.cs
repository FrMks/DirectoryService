using DirectoryService.Application.Locations;
using DirectoryService.Application.Locations.Validation;
using DirectoryService.Contracts.Locations;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services
            .AddScoped<ICreateLocationHandler, CreateLocationHandler>()
            .AddTransient<IValidator<CreateLocationRequest>, CreateLocationDtoValidator>();

        return services;
    }
}