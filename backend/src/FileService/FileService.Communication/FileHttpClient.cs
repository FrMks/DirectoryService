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

    public async Task<Result<GetContentUrlResponse, Errors>> GetContentUrlAsync(Guid fileId, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await HttpClient.GetAsync(
            $"files/{fileId:D}/content-url",
            cancellationToken);

            return await HandleResponseAsync<GetContentUrlResponse>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting media asset for {fileId}", fileId);
            return Error.Failure("server.internal", "Failed to request medias assets info").ToErrors();
        }
    }

    public Task<Result<IReadOnlyList<FileResponse>, Errors>> GetFilesByOwnerAsync(string context, Guid entityID, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<Result<FileResponse, Errors>> GetMideAssetById(Guid mediaAssetId, CancellationToken cancellationToken) => throw new NotImplementedException();
}
