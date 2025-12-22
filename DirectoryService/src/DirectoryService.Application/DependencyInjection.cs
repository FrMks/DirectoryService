using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Departments;
using DirectoryService.Application.Departments.Validation;
using DirectoryService.Application.Locations.Validation;
using DirectoryService.Application.Positions.Validation;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Locations;
using DirectoryService.Contracts.Positions;
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
        services.AddTransient<IValidator<GetLocationsRequest>, GetLocationsDtoValidator>();
        
        services.AddTransient<IValidator<CreateDepartmentRequest>, CreateDepartmentDtoValidator>();
        services.AddTransient<IValidator<UpdateDepartmentLocationsRequest>, UpdateDepartmentLocationsDtoValidator>();
        services.AddTransient<IValidator<UpdateParentLevelRequest>, UpdateParentLevelDtoValidator>();
        
        services.AddTransient<IValidator<CreatePositionRequest>, CreatePositionDtoValidator>();
        return services;
    }
}