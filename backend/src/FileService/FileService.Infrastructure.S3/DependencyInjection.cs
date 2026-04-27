using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
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
        services.AddScoped<IFileStorage, S3Provider>();

        S3Options s3Options = configuration.GetSection(nameof(S3Options)).Get<S3Options>()
            ?? throw new ApplicationException($"Failed to bind {nameof(S3Options)} from configuration.");

        // var options = new AWSOptions
        // {
        //     DefaultClientConfig =
        //     {
        //         ServiceURL = s3Options.Endpoint,
        //         UseHttp = !s3Options.WithSSL,
        //     },
        //     Credentials = new BasicAWSCredentials(s3Options.AccessKey, s3Options.SecretKey),
        // };

        // services.AddAWSService<IAmazonS3>(options);

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var s3Option = sp.GetRequiredService<IOptions<S3Options>>().Value;

            var config = new AmazonS3Config
            {
                ServiceURL = s3Option.Endpoint,
                UseHttp = !s3Option.WithSSL,
                ForcePathStyle = true,
            };

            return new AmazonS3Client(s3Option.AccessKey, s3Option.SecretKey, config);
        });

        return services;
    }
}
