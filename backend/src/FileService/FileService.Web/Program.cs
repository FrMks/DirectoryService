using FileService.Core;
using FileService.Core.Files;
using FileService.Core.Multipart;
using FileService.Core.UploadAndCompleteOnlyOneUrl;
using FileService.Infrastructure.Postgres;
using FileService.Infrastructure.Postgres.Repositories;
using FileService.Infrastructure.S3;
using FileService.Web;
using Microsoft.EntityFrameworkCore;
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

builder.Services.AddScoped<FileServiceDbContext>(_ =>
    new FileServiceDbContext(builder.Configuration.GetConnectionString("FileServiceDb")!));

builder.Services.AddScoped<IMediaRepository, MediaRepository>();

if (!string.IsNullOrWhiteSpace(seqConnectionString))
{
    loggerConfiguration.WriteTo.Seq(seqConnectionString);
}

Log.Logger = loggerConfiguration.CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

using (var scope = app.Services.CreateAsyncScope())
{
    IS3BucketInitializer bucketInitializer = scope.ServiceProvider.GetRequiredService<IS3BucketInitializer>();
    await bucketInitializer.InitializeAsync();
}

app.UseSharedExceptionHandling();

app.UseHttpLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "FileService"));

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<FileServiceDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseSerilogRequestLogging();

app.UseCors(FileService.Web.DependencyInjection.GetClientCorsPolicyName());

app.UseHttpsRedirection();

UploadEndpoint.MapFileEndpoints(app);
GetDownloadUrlEndpoint.MapFileEndpoints(app);
StartMultipartUpload.MapFileEndpoints(app);
CompleteMultipartUpload.MapFileEndpoints(app);
DeleteFileEndpoint.MapDeleteFileEndpoint(app);
UploadWithoutIFormFile.MapFileEndpoints(app);

app.Run();

namespace FileService.Web
{
    public partial class Program;
}
