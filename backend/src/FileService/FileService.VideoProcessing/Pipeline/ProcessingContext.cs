using FileService.Domain.Entities;
using FileService.Domain.MediaProcessing;

namespace FileService.VideoProcessing.Pipeline;

public sealed record ProcessingContext
{
    public required VideoProcess VideoPorcess { get; init; }

    public required VideoAsset VideoAsset { get; init; }

    public string? WorkingDirectory { get; private set; }

    // где будут генерироваться в нашей файловой системе hls файлы
    public string? HlsOutputDirectory { get; private set; }

    public string? MediaAssetUrl { get; set; }
}