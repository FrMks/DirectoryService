using FileService.Core;
using FileService.Core.Files;
using FileService.Core.Multipart;
using FileService.Core.UploadAndCompleteOnlyOneUrl;
using FileService.Infrastructure.Postgres;
using FileService.Infrastructure.Postgres.Database;
using FileService.Infrastructure.Postgres.Initializers;
using FileService.Infrastructure.Postgres.Repositories;
using FileService.Infrastructure.S3;
using FileService.Web;
using CrystalQuartz.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Serilog;
using Serilog.Events;
using Shared.Core.Database;
using Shared.Framework.Middlewares;
using FileService.Infrastructure.Postgres.Outbox;
using FileService.Core.Outbox;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<QuartzDbInitializer>();
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

builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection(OutboxOptions.SectionName));
builder.Services.AddHostedService<ProcessingJobOutboxWorker>();
builder.Services.AddHostedService<ProcessingJobOutboxRecoveryWorker>();
builder.Services.AddHostedService<ProcessingRetryOutboxWorker>();
builder.Services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
builder.Services.AddScoped<IProcessingRetryOutboxRepository, ProcessingRetryOutboxRepository>();
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<IVideoProcessingRepository, VideoPorcessingRepository>();
builder.Services.AddScoped<ITransactionManager, TransactionManager>();

if (!string.IsNullOrWhiteSpace(seqConnectionString))
{
    loggerConfiguration.WriteTo.Seq(seqConnectionString);
}

Log.Logger = loggerConfiguration.CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

var quartzDbInitializer = app.Services.GetRequiredService<QuartzDbInitializer>();
await quartzDbInitializer.InitializeAsync(app.Lifetime.ApplicationStopping);

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

app.UseRouting();
app.UseAuthorization();
app.UseCrystalQuartz(() => app.Services.GetRequiredService<ISchedulerFactory>().GetScheduler());

UploadEndpoint.MapFileEndpoints(app);
GetDownloadUrlEndpoint.MapFileEndpoints(app);
StartMultipartUpload.MapFileEndpoints(app);
CompleteMultipartUpload.MapFileEndpoints(app);
DeleteFileEndpoint.MapDeleteFileEndpoint(app);
UploadWithoutIFormFile.MapFileEndpoints(app);
CompleteUpload.MapFileEndpoints(app);
GetContentUrl.MapFileEndpoints(app);
GetFileById.MapFileEndpoints(app);
GetFilesByTargetEntity.MapFileEndpoints(app);
CancelPendingUpload.MapFileEndpoints(app);
AbortMultipartUpload.MapFileEndpoints(app);

app.Run();

namespace FileService.Web
{
    public partial class Program;
}
