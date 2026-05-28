using Amazon.Extensions.NETCore.Setup;
using Amazon.S3;
using FileService.Core.Files;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.S3;

public static class DependencyInjection
{
    public static IServiceCollection AddS3(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<S3Options>(configuration.GetSection(nameof(S3Options)));

        services.AddScoped<IS3Provider, S3Provider>();

        services.AddSingleton<IS3BucketInitializer, S3BucketInitializer>();

        services.AddDefaultAWSOptions(sp =>
        {
            var s3Options = sp.GetRequiredService<IOptions<S3Options>>().Value;

            return new AWSOptions
            {
                Credentials = new Amazon.Runtime.BasicAWSCredentials(
                    s3Options.AccessKey,
                    s3Options.SecretKey),
                DefaultClientConfig =
                {
                    ServiceURL = s3Options.Endpoint,
                    UseHttp = !s3Options.WithSsl,
                },
            };
        });

        services.AddAWSService<IAmazonS3>();

        services.AddTransient<IChunkSizeCalculator, ChunkSizeCalculator>();

        return services;
    }
}
