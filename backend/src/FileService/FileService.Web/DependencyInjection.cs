using FileService.Core;
using Microsoft.OpenApi.Models;

namespace FileService.Web;

public static class DependencyInjection
{
    private static string ClientCorsPolicy = "ClientCorsPolicy";

    public static IServiceCollection AddProgramDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddWebDependencies(configuration)
            .AddCore(configuration)
            .AddSerilog()
            .AddCors(options =>
            {
                options.AddPolicy(ClientCorsPolicy, policy =>
                {
                    policy
                        .WithOrigins("http://localhost:3000")
                        .AllowCredentials()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
    }

    public static string GetClientCorsPolicyName() => ClientCorsPolicy;

    private static IServiceCollection AddWebDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        var swaggerServerUrl = configuration["Swagger:ServerUrl"];

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.IncludeFields = true;
            });

        services.AddHttpLogging();

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                if (!string.IsNullOrWhiteSpace(swaggerServerUrl))
                {
                    document.Servers =
                    [
                        new OpenApiServer
                        {
                            Url = swaggerServerUrl,
                        },
                    ];
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
