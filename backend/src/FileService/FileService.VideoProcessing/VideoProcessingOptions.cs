namespace FileService.VideoProcessing;

public sealed record VideoProcessingOptions
{
    public const string SECTION_NAME = "VideoProcessing";

    public string FfmpegPath { get; init; } = "ffmpeg";

    public string FfprobePath { get; init; } = "ffprobe";

    public bool UseHardwareAcceleration { get; init; }

    public string VideoEncoder { get; init; } = "libx264";

    public string VideoPreset { get; init; } = "medium";

    public int UploadDegreeOfParallelism { get; init; } = 10;

    public int MaxRetries { get; init; } = 3;

    public int RetryDelaySeconds { get; init; } = 3;

}