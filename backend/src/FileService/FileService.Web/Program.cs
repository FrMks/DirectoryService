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
using FileService.Core.Processing;
using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.Postgresql;
using FileService.Contracts.IntegrationEvents;
using FileService.Web.EventRouting;

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

// RabbitMQ configuration
var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMq")
    ?? throw new InvalidOperationException("RabbitMQ connection string is not configured.");

builder.Host.UseWolverine(options =>
{
    options.UseRabbitMq(new Uri(rabbitMqConnectionString))
        .AutoProvision(); // Автоматическое создание необходимых ресурсов в RabbitMQ (очередей, обменников и т.д.) при запуске приложения.

    // RabbitMQ - транспорт (под этим подразумевается, что сообщения будут передаваться через RabbitMQ),
    // а Postgres - надежное хранилище исходящих и входящих сообщений 
    // (под этим подразумевается, что сообщения будут сохраняться в базе данных Postgres для обеспечения надежности и возможности повторной обработки).
    options.PersistMessagesWithPostgresql(
        builder.Configuration.GetConnectionString("FileServiceDb")!,
        "wolverine");

    // Нужно чтобы все сообщения, отправляемые из всех конечных точек (endpoints), использовали надежный Outbox (Durable Outbox).
    // Это работает так: когда сообщение отправляется из конечной точки, оно сначала сохраняется в базе данных (Postgres) в таблице Outbox.
    // Затем, когда транзакция завершается успешно, сообщение извлекается из таблицы Outbox и отправляется в RabbitMQ.
    // Под отправкой сообщения тут понимается отправка сообщения из Handler в RabbitMQ.
    options.Policies.UseDurableOutboxOnAllSendingEndpoints();

    // Настройка маршрутизации сообщений в RabbitMQ
    // Здесь мы указываем, что сообщения типа FileReady будут публиковаться в RabbitMQ Exchange с именем, определенным в FileEventsRouting.ExchangeName.
    // RabbitMQ Exchange - это точка маршрутизации сообщений, которая определяет, как сообщения будут направляться к соответствующим очередям.
    options.PublishMessagesToRabbitMqExchange<FileReady>(
        FileEventsRouting.ExchangeName,
        fileReady => FileEventsRouting.FileReady(fileReady.TargetEntityType));

    options.PublishMessagesToRabbitMqExchange<FileDeleted>(
        FileEventsRouting.ExchangeName,
        fileDeleted => FileEventsRouting.FileDeleted(fileDeleted.TargetEntityType));

    options.PublishMessagesToRabbitMqExchange<VideoProcessingCompleted>(
        FileEventsRouting.ExchangeName,
        videoProcessingCompleted => FileEventsRouting.VideoProcessingCompleted(videoProcessingCompleted.TargetEntityType));

    options.PublishMessagesToRabbitMqExchange<VideoProcessingFailed>(
        FileEventsRouting.ExchangeName,
        videoProcessingFailed => FileEventsRouting.VideoProcessingFailed(videoProcessingFailed.TargetEntityType));
});

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
GetVideoProcessingStatus.MapVideoProcessingStatusEndpoint(app);
StreamVideoProcessingStatus.MapVideoProcessingStatusStreamEndpoint(app);

app.Run();

namespace FileService.Web
{
    public partial class Program;
}
