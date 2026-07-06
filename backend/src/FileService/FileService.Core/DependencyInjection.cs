using FileService.Core.Cache;
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
        services.AddScoped<CancelPendingUploadHandler>();
        services.AddScoped<AbortMultipartUploadHandler>();
        services.AddScoped<DowloadUrlCacheService>();

        services.Configure<DownloadUrlCacheOptions>(
            configuration.GetSection(DownloadUrlCacheOptions.SectionName));

        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            return services;
        }

        services.AddStackExchangeRedisCache(setup =>
        {
            setup.Configuration = redisConnectionString;
        });

        DownloadUrlCacheOptions downloadUrlCacheSection = configuration
            .GetSection(DownloadUrlCacheOptions.SectionName)
            .Get<DownloadUrlCacheOptions>() ?? new DownloadUrlCacheOptions();

        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                LocalCacheExpiration = TimeSpan.FromMinutes(downloadUrlCacheSection.ExpirationMinutes),
                Expiration = TimeSpan.FromMinutes(downloadUrlCacheSection.ExpirationMinutes),
            };
        });

        return services;
    }
}
