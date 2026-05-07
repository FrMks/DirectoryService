using CSharpFunctionalExtensions;
using Shared;

namespace FileService.Domain;

/// <summary>
/// Файл нельзя проверять только по расширению. Пользователь может отправить video.mp4, 
/// но Content-Type будет image/png. Domain должен такое поймать
/// </summary>
public sealed record ContentType
{
    public string Value { get; }

    public MediaType Category { get; }

    private ContentType(string value, MediaType mediaType)
    {
        Value = value;
        Category = mediaType;
    }

    public static Result<ContentType, Error> Create(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return Error.Validation("value.is.null", nameof(contentType));

        MediaType category = contentType switch
        {
            _ when contentType.Contains("video", StringComparison.InvariantCultureIgnoreCase) => MediaType.VIDEO,
            _ when contentType.Contains("image", StringComparison.InvariantCultureIgnoreCase) => MediaType.IMAGE,
            _ when contentType.Contains("audio", StringComparison.InvariantCultureIgnoreCase) => MediaType.AUDIO,
            _ => MediaType.UNKNOWN,
        };

        return new ContentType(contentType, category);
    }
}
