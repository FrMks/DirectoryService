using System.Linq.Expressions;
using FileService.Core.Cache;
using FileService.Core.Files.FileKey;
using FileService.Core.Multipart;
using FileService.Core.UploadAndCompleteOnlyOneUrl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

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
        services.AddScoped<DownloadUrlCacheService>();

        services.Configure<DownloadUrlCacheOptions>(
            configuration.GetSection(DownloadUrlCacheOptions.SectionName));

        string? redisConnectionString = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(setup =>
            {
                setup.Configuration = redisConnectionString;
            });
        }

        services.AddHybridCache();

        services.AddQuartzServices(configuration);

        return services;
    }

    public static IServiceCollection AddQuartzServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddQuartz(options =>
        {
            options.UsePersistentStore(persistenceOptions =>
            {
                persistenceOptions.UsePostgres(cfg =>
                {
                    cfg.ConnectionString = configuration.GetConnectionString("FileServiceDb")!;
                });

                persistenceOptions.UseNewtonsoftJsonSerializer();
                persistenceOptions.UseProperties = true;
            });

            var testJobKey = new JobKey("TestJob");
            options.AddJob<TestJob>(opts => opts.WithIdentity(testJobKey));

            options.AddTrigger(opts => opts
                .ForJob(testJobKey)
                .WithIdentity("TestJob-trigger")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(1)
                    .RepeatForever()));
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        services.AddSilkierQuartz(
            options =>
            {
                options.VirtualPathRoot = "/quartz";
                options.UseLocalTime = true;
                options.DefaultDateFormat = "yyyy-MM-dd";
                options.DefaultTimeFormat = "HH:mm:ss";
            },
            authenticationOptions =>
            {
                authenticationOptions.AccessRequirement =
                    SilkierQuartz.SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowAnonymous;
            });

        return services;
    }
}
