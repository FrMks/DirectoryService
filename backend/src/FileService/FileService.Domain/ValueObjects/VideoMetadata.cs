using CSharpFunctionalExtensions;
using Shared;

namespace FileService.Domain.ValueObjects;

public sealed record VideoMetadata
{
    public TimeSpan Duration { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public string Codec { get; init; } = null!;

    public string Container { get; init; } = null!;

    // EF Core
    private VideoMetadata()
    {
    }

    private VideoMetadata(
        TimeSpan duration,
        int width,
        int height,
        string codec,
        string container)
    {
        Duration = duration;
        Width = width;
        Height = height;
        Codec = codec;
        Container = container;
    }

    public static Result<VideoMetadata, Error> Create(
        TimeSpan duration,
        int width,
        int height,
        string codec,
        string container)
    {
        if (duration <= TimeSpan.Zero)
            return Error.Validation("create.video.metadata", "Duration should be greater than zero");

        if (width <= 0 || height <= 0)
            return Error.Validation("create.video.metadata", "Width and Height should be greater than zero");

        return new VideoMetadata(duration, width, height, codec, container);
    }
}