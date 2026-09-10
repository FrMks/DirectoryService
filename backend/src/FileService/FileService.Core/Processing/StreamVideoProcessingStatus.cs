using System.Text.Json;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared;
using Shared.Framework.EndpointResults;

namespace FileService.Core.Processing;

public static class StreamVideoProcessingStatus
{
    private const string Ready = "ready";
    private const string Failed = "failed";
    private const string Deleted = "deleted";

    public static IEndpointRouteBuilder MapVideoProcessingStatusStreamEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/files/{videoAssetId:guid}/processing-status/stream",
            async Task<Microsoft.AspNetCore.Http.IResult> (
                [FromRoute] Guid videoAssetId,
                [FromServices] GetVideoProcessingStatusHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
        {
            Result<VideoProcessingStatusResponse, Error> initialResult =
                await handler.Handle(videoAssetId, cancellationToken);
            if (initialResult.IsFailure)
            {
                return new ErrorsResult(initialResult.Error);
            }

            // Сообщает клиенту, что ответ содержит SSE-события
            httpContext.Response.ContentType = "text/event-stream";
            // Запрещает браузеру или proxe кэшировать поток
            httpContext.Response.Headers["Cache-Control"] = "no-cache";
            // Отключает буферизацию ответа в Nginx
            httpContext.Response.Headers["X-Accel-Buffering"] = "no";

            try
            {
                VideoProcessingStatusResponse lastStatus = initialResult.Value;

                await WriteProgressEventAsync(
                    httpContext.Response,
                    "progress",
                    initialResult.Value,
                    cancellationToken);

                if (IsTerminal(lastStatus))
                {
                    return Results.Empty;
                }

                TimeSpan pollingInterval = TimeSpan.FromSeconds(1);
                TimeSpan heartbeatInterval = TimeSpan.FromSeconds(15);
                DateTimeOffset lastWriteAt = DateTimeOffset.UtcNow;

                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(pollingInterval, cancellationToken);

                    Result<VideoProcessingStatusResponse, Error> currentResult
                        = await handler.Handle(videoAssetId, cancellationToken);
                    if (currentResult.IsFailure)
                    {
                        break;
                    }

                    VideoProcessingStatusResponse currentStatus = currentResult.Value;

                    if (currentStatus != lastStatus)
                    {
                        await WriteProgressEventAsync(
                            httpContext.Response,
                            "progress",
                            currentResult.Value,
                            cancellationToken);

                        lastStatus = currentResult.Value;
                        lastWriteAt = DateTimeOffset.UtcNow;
                    }

                    if (IsTerminal(lastStatus))
                    {
                        break;
                    }

                    if (DateTimeOffset.UtcNow - lastWriteAt >= heartbeatInterval)
                    {
                        await WriteHeartbeatAsync(
                            httpContext.Response,
                            cancellationToken);

                        lastWriteAt = DateTimeOffset.UtcNow;
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Клиент отключился. Background job продолжает работать.
            }

            return Results.Empty;
        });

        return endpoints;
    }

    private static async Task WriteProgressEventAsync(
        HttpResponse response,
        string eventName,
        object data,
        CancellationToken cancellationToken)
    {
        await response.WriteAsync($"event: {eventName}\n", cancellationToken);
        await response.WriteAsync("data: ", cancellationToken);
        await JsonSerializer.SerializeAsync(response.Body, data, JsonSerializerOptions.Web, cancellationToken);
        // Одно событие выглядит так:
        // event: progress\n
        // data: {"percent":10}\n
        // \n - обязательный символ конца события, иначе браузер не вызовет обработчик события
        // без последнего \n браузер не вызовет обработчик события, а будет ждать следующего события
        await response.WriteAsync("\n\n", cancellationToken);
        // Просит сервер немедленно отправитьть уже записанные данные клиенту
        // без flush данные не пропадут, но могут остаться во внутреннем буфере
        await response.Body.FlushAsync(cancellationToken);
        // Получившийся HTTP body будет выглядеть так:
        // event: progress
        // data: {"assetId":"...","status":"processing","currentStep":"generate_hls","percent":10,"errorCode":null}
    }

    // True - видео готово, новых изменений не будет / 
    // обработка окончательно завершилась ошибокой /
    // asset больше не должен обработываться (удалён)
    private static bool IsTerminal(VideoProcessingStatusResponse response)
    {
        if (response.Status is Ready or Failed or Deleted)
        {
            return true;
        }
        return false;
    }

    private static async Task WriteHeartbeatAsync(HttpResponse response, CancellationToken cancellationToken)
    {
        await response.WriteAsync(": heartbeat\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}