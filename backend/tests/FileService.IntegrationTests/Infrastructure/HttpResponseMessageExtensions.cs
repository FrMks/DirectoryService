using System.Net.Http.Json;
using Shared;

namespace FileService.IntegrationTests.Infrastructure;

internal static class HttpResponseMessageExtensions
{
    public static async Task<T> ReadEnvelopeResultAsync<T>(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
        where T : class
    {
        Envelope<T>? envelope = await response.Content
            .ReadFromJsonAsync<Envelope<T>>(cancellationToken);

        return envelope?.Result
            ?? throw new InvalidOperationException("Response does not contain an envelope result.");
    }
}
