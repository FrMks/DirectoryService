using Shared.Core.Abstractions;
using DirectoryService.Application.Departments.SoftDeleteDepartment;
using DirectoryService.Application.Departments.Validation;
using DirectoryService.Application.Locations.Validation;
using DirectoryService.Application.Positions.Validation;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Locations;
using DirectoryService.Contracts.Locations.GetLocations;
using DirectoryService.Contracts.Positions;
using FluentValidation;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var assembblie = typeof(DependencyInjection).Assembly;
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new ArgumentNullException("ConnectionStrings:Redis");

        services.Scan(scan => scan.FromAssemblies(assembblie)
            .AddClasses(classes => classes
                .AssignableToAny(typeof(ICommandHandler<,>), typeof(ICommandHandler<>)))
            .AsSelfWithInterfaces()
            .WithScopedLifetime());

        services.Scan(scan => scan.FromAssemblies(assembblie)
            .AddClasses(classes => classes
                .AssignableToAny(typeof(IQueryHandler<,>), typeof(IQueryHandler<>)))
            .AsSelfWithInterfaces()
            .WithScopedLifetime());

        services.AddTransient<IValidator<CreateLocationRequest>, CreateLocationDtoValidator>();
        services.AddTransient<IValidator<GetLocationsRequest>, GetLocationsDtoValidator>();

        services.AddTransient<IValidator<CreateDepartmentRequest>, CreateDepartmentDtoValidator>();
        services.AddTransient<IValidator<UpdateDepartmentLocationsRequest>, UpdateDepartmentLocationsDtoValidator>();
        services.AddTransient<IValidator<UpdateParentLevelRequest>, UpdateParentLevelDtoValidator>();
        services.AddTransient<IValidator<SoftDeleteDepartmentCommand>, SoftDeleteDepartmentCommandValidator>();

        services.AddTransient<IValidator<CreatePositionRequest>, CreatePositionDtoValidator>();

        services.AddStackExchangeRedisCache(setup =>
        {
            setup.Configuration = redisConnectionString;
        });

        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                LocalCacheExpiration = TimeSpan.FromMinutes(5), // Локально в памяти текущего процесса (живет внутри запущенного экземпляра приложения)
                Expiration = TimeSpan.FromMinutes(5), // Общий срок жизни записи (обычно Redis, IDistributedCache)
                // Если нет в локальном, то будет запрашивать из удаленного, и если там есть, то положит в локальный кэш
            };
        });

        return services;
    }
}
