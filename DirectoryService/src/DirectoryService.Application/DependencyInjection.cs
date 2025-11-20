using DirectoryService.Application.Abstractions;
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
        var assembblie = typeof(DependencyInjection).Assembly;

        services.Scan(scan => scan.FromAssemblies(assembblie)
            .AddClasses(classes => classes
                .AssignableToAny(typeof(ICommandHandler<,>), typeof(ICommandHandler<>)))
            .AsSelfWithInterfaces()
            .WithScopedLifetime());

        services.AddTransient<IValidator<CreateLocationRequest>, CreateLocationDtoValidator>();
        return services;
    }
}