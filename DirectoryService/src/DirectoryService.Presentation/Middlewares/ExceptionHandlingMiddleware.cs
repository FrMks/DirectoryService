using System.Text.Json;
using DirectoryService.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Shared;

namespace DirectoryService.Web.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, exception.Message);

        (int code, Errors errors) = exception switch
        {
            BadRequestException badRequest =>
                (StatusCodes.Status400BadRequest,
                    TryDeserializeErrors(badRequest.Message)),

            NotFoundException notFound =>
                (StatusCodes.Status404NotFound,
                    TryDeserializeErrors(notFound.Message)),

            _ =>
                (StatusCodes.Status500InternalServerError,
                    new Errors([Error.Failure("server.error", "Something went wrong")])),
        };

        var envelope = Envelope.Error(errors);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = code;

        await context.Response.WriteAsJsonAsync(envelope);
    }

    private static Errors TryDeserializeErrors(string message)
    {
        try
        {
            var errorArray = JsonSerializer.Deserialize<Error[]>(message);
            return errorArray != null ? new Errors(errorArray) :
                    new Errors([Error.Failure("deserialization.error", "Failed to parse Error message")]);
        }
        catch
        {
            return new Errors([Error.Failure("unknown.error", message)]);
        }
    }
}

public static class ExceptionHandlingMiddlewareExtension
{
    public static IApplicationBuilder UseExceptionHandlingMiddleware(this WebApplication application) =>
        application.UseMiddleware<ExceptionHandlingMiddleware>();
}