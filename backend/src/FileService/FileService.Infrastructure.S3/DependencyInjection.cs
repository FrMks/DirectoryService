using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.S3;

public static class DependencyInjection
{
    public static IServiceCollection AddS3Infrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<S3Options>(configuration.GetSection(nameof(S3Options)));

        S3Options s3Options = configuration.GetSection(nameof(S3Options)).Get<S3Options>()
            ?? throw new ApplicationException($"Failed to bind {nameof(S3Options)} from configuration.");

        var options = new AWSOptions
        {
            DefaultClientConfig =
            {
                ServiceURL = s3Options.Endpoint,
                UseHttp = !s3Options.WithSSL,
            },
            Credentials = new BasicAWSCredentials(s3Options.AccessKey, s3Options.SecretKey),
        };

        services.AddAWSService<IAmazonS3>(options);

        return services;
    }
}