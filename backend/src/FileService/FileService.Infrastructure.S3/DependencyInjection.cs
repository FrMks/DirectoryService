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

        var options = new AWSOptions
        {
            DefaultClientConfig =
            {
                ServiceURL = "http://localhost:9000",
                UseHttp = true,
            },
            Credentials = new BasicAWSCredentials("minioadmin", "minioadmin"),
        };

        services.AddAWSService<IAmazonS3>(options);

        return services;
    }
}