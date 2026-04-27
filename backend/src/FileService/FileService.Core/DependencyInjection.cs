using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Abstractions;

namespace FileService.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        var redisConnectionString = configuration.GetConnectionString("Redis");

        services.Scan(scan => scan.FromAssemblies(assembly)
            .AddClasses(classes => classes
                .AssignableToAny(typeof(ICommandHandler<,>), typeof(ICommandHandler<>)))
            .AsSelfWithInterfaces()
            .WithScopedLifetime());

        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            return services;
        }

        services.AddStackExchangeRedisCache(setup =>
        {
            setup.Configuration = redisConnectionString;
        });

        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                LocalCacheExpiration = TimeSpan.FromMinutes(5),
                Expiration = TimeSpan.FromMinutes(5),
            };
        });

        return services;
    }
}
