using CSharpFunctionalExtensions;
using FileService.Domain.Entities;
using FileService.Domain.MediaProcessing;
using Microsoft.Extensions.DependencyInjection;
using Shared;

namespace FileService.VideoProcessing.Pipeline;

/// <summary>
/// Объект, который несет данные, нужные pipeline во время обработки видео.
/// </summary>
public sealed record ProcessingContext
{
    private const string HLS_SUBDIRECTORY = "hls";

    public required VideoProcess VideoProcess { get; init; }

    public required VideoAsset VideoAsset { get; init; }

    // Корневая временная папка всей обработки: туда можно класть исходик, превью, логи ffmpeg, промежуточные файлы, metadata и т.п.
    public string? WorkingDirectory { get; private set; }

    // Специальная подпапка только для HLS-результата: .m3u8. Это удобно, потому что
    // upload step может взять именно эту папка и не рисковать залить вместе с HLS какие-нибудь временные служебные файлы
    public string? HlsOutputDirectory { get; private set; }

    /// <summary>
    /// Временный Url для скачивания, который указывает на объект в S3/MinIO, но это не идентификатор стабильного хранилища.
    /// </summary>
    public string? MediaAssetUrl { get; private set; }

    /// <summary>
    /// Путь в explorer на скаченный файл, с которым будет работать ffmpeg.
    /// </summary>
    public string? SourceFilePath { get; private set; }

    public void SetSourceFilePath(string path)
    {
        SourceFilePath = path;
    }

    // Windows: C:\Users\<User>\AppData\Local\Temp\video-processing<unique> (Создаем уникальную папку ВНУТРИ Temp)
    // macOS: /var/folders/.../T/video-processing<unique>
    // Linux: /tmp/video-processing<unique>
    public UnitResult<Error> CreateWorkingDirectory()
    {
        try
        {
            // temp root for example: C:\Users\coder8\AppData\Local\Temp
            // C:\Users\coder8\AppData\Local\Temp\video-processingabc123
            WorkingDirectory = Directory.CreateTempSubdirectory("video-processing").FullName;

            // C:\Users\coder8\AppData\Local\Temp\video-processingabc123\hls
            HlsOutputDirectory = Path.Combine(WorkingDirectory, HLS_SUBDIRECTORY);
            Directory.CreateDirectory(HlsOutputDirectory);
        }
        catch (Exception ex)
        {
            return Error.Failure("working.directory.creation", $"Failed to cretae working directory: {ex.Message}");
        }

        return UnitResult.Success<Error>();
    }

    public void SetMediaAssetUrl(string url)
    {
        MediaAssetUrl = url;
    }

    internal void Cleanup()
    {
        WorkingDirectory = null;
        HlsOutputDirectory = null;
        MediaAssetUrl = null;
        SourceFilePath = null;
    }
}
