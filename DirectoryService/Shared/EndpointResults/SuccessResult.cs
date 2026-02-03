using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.EndpointResults;

public class SuccessResult<TValue> : IResult
{
    private readonly TValue _value;

    public SuccessResult(TValue value)
    {
        _value = value;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var envelope = Envelope.Ok(_value);
        httpContext.Response.StatusCode = (int)HttpStatusCode.OK;

        // Получаем настройки JSON из DI
        var jsonOptions = httpContext.RequestServices
            .GetService<Microsoft.AspNetCore.Http.Json.JsonOptions>();

        // Используем глобальные настройки или дефолтные с IncludeFields
        var options = jsonOptions?.SerializerOptions ?? new JsonSerializerOptions
        {
            IncludeFields = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return httpContext.Response.WriteAsJsonAsync(envelope, options);
    }
}