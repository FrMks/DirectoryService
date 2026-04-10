using DirectoryService.Application;
using Microsoft.OpenApi.Models;
using Serilog;
using Shared;

namespace DirectoryService.Web;

public static class DependencyInjection // docker compose up --build для разворачивания приложения. http://localhost:8080
{
    public static IServiceCollection AddProgramDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddWebDependencies()
            .AddApplication(configuration)
            .AddSerilog();
    }

    private static IServiceCollection AddWebDependencies(this IServiceCollection services)
    {
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
                document.Servers =
                [
                    new OpenApiServer
                    {
                        Url = "http://localhost:8080",
                    },
                ];

                return Task.CompletedTask;
            });

            options.AddSchemaTransformer((schema, context, _) =>
            {
                if (context.JsonTypeInfo.Type == typeof(Envelope<Errors>))
                {
                    if (schema.Properties.TryGetValue("errors", out var errorsProp))
                    {
                        errorsProp.Items.Reference = new OpenApiReference
                        {
                            Type = ReferenceType.Schema,
                            Id = "Error",
                        };
                    }
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
