using FileService.Core.Files;
using FileService.Core.Files.FileKey;
using FileService.Core.Multipart;
using FileService.Core.UploadAndCompleteOnlyOneUrl;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileService.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<StartMultipartUploadHandler>();
        services.AddScoped<CompleteMultipartUploadHandler>();
        services.AddSingleton<IFileKeyGenerator, FileKeyGenerator>();
        services.AddScoped<StartUploadHandler>();
        services.AddScoped<CompleteUploadHandler>();
        services.AddScoped<GetContentUrlHandler>();
        services.AddScoped<GetFileByIdHandler>();
        services.AddScoped<GetFilesByTargetEntityHandler>();

        var redisConnectionString = configuration.GetConnectionString("Redis");

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
