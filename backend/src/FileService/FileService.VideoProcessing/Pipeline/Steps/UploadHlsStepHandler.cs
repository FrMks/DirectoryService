using CSharpFunctionalExtensions;
using FileService.Core.Multipart;
using FileService.Domain.Errors;
using FileService.Domain.MediaProcessing;
using FileService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;

namespace FileService.VideoProcessing.Pipeline.Steps;

public class UploadHlsStepHandler : IProcessingStepHandler
{
    private readonly IS3Provider _s3Provider;
    private readonly IOptions<VideoProcessingOptions> _options;
    private readonly ILogger<UploadHlsStepHandler> _logger;

    public UploadHlsStepHandler(
        IS3Provider s3Provider,
        IOptions<VideoProcessingOptions> options,
        ILogger<UploadHlsStepHandler> logger)
    {
        _s3Provider = s3Provider;
        _options = options;
        _logger = logger;
    }

    public StepType StepType => StepType.UPLOAD_HLS;

    public async Task<Result<ProcessingContext, Error>> ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Uploading HLS to S3 for VideoAssetId: {VideoAssetId}",
            context.VideoAsset.Id);

        if (string.IsNullOrWhiteSpace(context.HlsOutputDirectory))
            return FileError.HlsProcessingFailed("HLS output directory is not set");

        if (!Directory.Exists(context.HlsOutputDirectory))
            return FileError.HlsProcessingFailed("HLS output directory does not exist");

        // "*.*" - все файлы, которые содержат точку
        string[] hlsFiles = Directory.GetFiles(context.HlsOutputDirectory, "*.*", SearchOption.TopDirectoryOnly);
        if (hlsFiles.Length == 0)
            return FileError.HlsProcessingFailed("No Hls files found in output directory");

        // Корневой ключ для всех HLS файлов для этого видео в S3/MinIO
        // videos/hls/111-111-111
        // Это как папка, в котором будут лежать HLS файлы
        Result<StorageKey, Error> hlsRootKey = context.VideoAsset.GetHlsRootKey();
        if (hlsRootKey.IsFailure)
            return hlsRootKey.Error;

        // Ограничиваем максимальным числом файлов, которые мы можем отправить
        using var throttler = new SemaphoreSlim(_options.Value.UploadDegreeOfParallelism);

        Task<UnitResult<Error>>[] uploadTasks = hlsFiles.Select(async file =>
        {
            await throttler.WaitAsync(cancellationToken);
            try
            {
                return await UploadHlsFileAsync(hlsRootKey.Value, file, cancellationToken);
            }
            finally
            {
                throttler.Release();
            }
        }).ToArray();

        UnitResult<Error>[] results = await Task.WhenAll(uploadTasks);

        UnitResult<Error> firstError = results.FirstOrDefault(r => r.IsFailure);
        if (firstError.IsFailure)
            return firstError.Error;

        _logger.LogInformation(
            "Successfully uploaded {FileCount} HLS files for VideoAssetId: {VideoAssetId}",
            hlsFiles.Length,
            context.VideoAsset.Id);

        // Конечный S3 ключ для master.m3u8
        Result<StorageKey, Error> masterPlaylistKey = context.VideoAsset.GetHlsMasterPlaylistKey();
        if (masterPlaylistKey.IsFailure)
            return masterPlaylistKey.Error;

        return context;
    }

    /// <param name="hlsRootKey">Где живет объект в s3/minio.</param>
    /// <param name="localFilePath">Путь на машине до файла.</param>
    private async Task<UnitResult<Error>> UploadHlsFileAsync(
        StorageKey hlsRootKey,
        string localFilePath,
        CancellationToken cancellationToken)
    {
        string fileName = Path.GetFileName(localFilePath);

        // Создаем конечный s3 ключ для этого файла.
        // Конечный S3 ключ внутри hlsRootKey
        Result<StorageKey, Error> storageKey = hlsRootKey.AppendKey(fileName);
        if (storageKey.IsFailure)
            return storageKey.Error;

        string contentType = GetContentType(localFilePath);

        await using FileStream fileStream = File.OpenRead(localFilePath);

        return await _s3Provider.UploadFileAsync(
            storageKey.Value,
            fileStream,
            contentType,
            cancellationToken);
    }

    private string GetContentType(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            // файл, который сообщает проигрывателю, какие варианты видео и файлы сегментов имеются
            ".m3u8" => "application/vnd.apple.mpegurl",
            // содержит небольшой фрагмент самого видео или аудио
            ".ts" => "video/mp2t",
            _ => "application/octet-stream",
        };
    }
}
