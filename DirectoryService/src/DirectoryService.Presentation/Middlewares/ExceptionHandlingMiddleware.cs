using System.Text.Json;
using DirectoryService.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Shared;

namespace DirectoryService.Web.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception e)
        {
            await HandleExceptionAsync(context, e);

            await context.Response.WriteAsJsonAsync(e.Message);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, exception.Message);

        (int code, Error[]? errors) = exception switch
        {
            BadRequestException =>
                (StatusCodes.Status500InternalServerError,
                    JsonSerializer.Deserialize<Error[]>(exception.Message)),
            NotFoundException =>
                (StatusCodes.Status404NotFound,
                    JsonSerializer.Deserialize<Error[]>(exception.Message)),
            _ =>
                (StatusCodes.Status500InternalServerError,
                    [Error.Failure(null, "Something went wrong")]),
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = code;

        await context.Response.WriteAsJsonAsync(errors);
    }
}

public static class ExceptionHandlingMiddlewareExtension
{
    public static IApplicationBuilder UseExceptionHandlingMiddleware(this WebApplication application) =>
        application.UseMiddleware<ExceptionHandlingMiddleware>();
}