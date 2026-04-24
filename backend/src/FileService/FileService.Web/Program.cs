using FileService.Web;
using Serilog;
using Serilog.Events;
using Shared.Framework.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProgramDependencies(builder.Configuration);

var seqConnectionString = builder.Configuration.GetConnectionString("Seq");
var loggerConfiguration = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Debug()
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning);

if (!string.IsNullOrWhiteSpace(seqConnectionString))
{
    loggerConfiguration.WriteTo.Seq(seqConnectionString);
}

Log.Logger = loggerConfiguration.CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

app.UseSharedExceptionHandling();

app.UseHttpLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "FileService"));
}

app.UseSerilogRequestLogging();

app.UseCors(DependencyInjection.GetClientCorsPolicyName());

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

namespace FileService.Web
{
    public partial class Program;
}
