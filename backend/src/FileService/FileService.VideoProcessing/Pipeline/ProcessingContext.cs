using FileService.Domain.Entities;
using FileService.Domain.MediaProcessing;

namespace FileService.VideoProcessing.Pipeline;

/// <summary>
/// Объект, который несет данные, нужные pipeline во время обработки видео.
/// </summary>
public sealed record ProcessingContext
{
    public required VideoProcess VideoProcess { get; init; }

    public required VideoAsset VideoAsset { get; init; }

    public string? WorkingDirectory { get; init; }

    // где будут генерироваться в нашей файловой системе hls файлы
    public string? HlsOutputDirectory { get; init; }

    public string? MediaAssetUrl { get; set; }
}