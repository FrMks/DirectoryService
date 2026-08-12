namespace FileService.VideoProcessing;

public sealed record VideoProcessingOptions
{
    public const string SECTION_NAME = "VideoProcessing";

    public string FfmpegPath { get; init; } = "ffmpeg";

    public string FfprobePath { get; init; } = "ffprobe";

    // Включает декодирование/кодирование через GPU, если хост и выбранный encoder это поддерживают.
    public bool UseHardwareAcceleration { get; init; }

    // Видео-кодек ffmpeg для выходных файлов, например libx264 для широкой совместимости H.264.
    public string VideoEncoder { get; init; } = "libx264";

    // Баланс скорости и качества кодирования: медленные presets обычно дают меньший размер или лучшее качество.
    public string VideoPreset { get; init; } = "medium";

    // Максимальное количество сгенерированных файлов, которые загружаются одновременно.
    public int UploadDegreeOfParallelism { get; init; } = 10;

    public int MaxRetries { get; init; } = 3;

    public int RetryDelaySeconds { get; init; } = 3;

    public int ProcessingTimeoutMinutes { get; init; } = 30;
}
