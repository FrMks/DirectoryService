using CSharpFunctionalExtensions;
using FileService.Contracts;
using Microsoft.Extensions.Logging;
using Shared;

namespace FileService.Communication;

internal sealed class FileHttpClient : BaseHttpClient, IFileCommunicationService
{
    public FileHttpClient(HttpClient httpClient, ILogger<FileHttpClient> logger)
        : base(httpClient, logger)
    {
    }

    public Task<Result<FileResponse, Errors>> GetMediaAssetByIdAsync(
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        return SendGetAsync<FileResponse>(
            $"files/{mediaAssetId:D}",
            cancellationToken);
    }

    public Task<Result<IReadOnlyList<FileResponse>, Errors>> GetFilesByOwnerAsync(
        string context,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context))
        {
            return Task.FromResult<Result<IReadOnlyList<FileResponse>, Errors>>(
                Error.Validation(
                    "file-service.context.required",
                    "File owner context is required").ToErrors());
        }

        string escapedContext = Uri.EscapeDataString(context);

        return SendGetAsync<IReadOnlyList<FileResponse>>(
            $"files?context={escapedContext}&contextId={entityId:D}",
            cancellationToken);
    }

    public Task<Result<GetContentUrlResponse, Errors>> GetContentUrlAsync(
        Guid fileId,
        CancellationToken cancellationToken)
    {
        return SendGetAsync<GetContentUrlResponse>(
            $"files/{fileId:D}/content-url",
            cancellationToken);
    }

    private async Task<Result<TResponse, Errors>> SendGetAsync<TResponse>(
        string requestUri,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        try
        {
            using HttpResponseMessage response = await HttpClient.GetAsync(
                requestUri,
                cancellationToken);

            return await HandleResponseAsync<TResponse>(response, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            Logger.LogWarning(ex, "File Service request timed out. Uri: {RequestUri}", requestUri);

            return Error.Failure(
                "file-service.timeout",
                "File Service request timed out").ToErrors();
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "File Service is unavailable. Uri: {RequestUri}", requestUri);

            return Error.Failure(
                "file-service.unavailable",
                "File Service is unavailable").ToErrors();
        }
        catch (Polly.Timeout.TimeoutRejectedException ex)
        {
            Logger.LogWarning(
                ex,
                "File Service request timed out. Uri: {RequestUri}",
                requestUri);

            return Error.Failure(
                "file-service.timeout",
                "File Service request timed out").ToErrors();
        }
        catch (Polly.CircuitBreaker.BrokenCircuitException ex)
        {
            Logger.LogWarning(
                ex,
                "File Service circuit breaker is open. Uri: {RequestUri}",
                requestUri);

            return Error.Failure(
                "file-service.unavailable",
                "File Service is temporarily unavailable").ToErrors();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected File Service client error. Uri: {RequestUri}", requestUri);

            return Error.Failure(
                "file-service.client.failure",
                "Failed to process File Service request").ToErrors();
        }
    }
}
