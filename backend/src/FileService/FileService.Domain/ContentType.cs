using CSharpFunctionalExtensions;
using Shared;

namespace FileService.Domain;

/// <summary>
/// Файл нельзя проверять только по расширению. Пользователь может отправить video.mp4, 
/// но Content-Type будет image/png. Domain должен такое поймать
/// </summary>
public sealed record ContentType
{
    public string Value { get; init; }

    public MediaType Category { get; init; }

    private ContentType() { }

    private ContentType(string value, MediaType category)
    {
        Value = value;
        Category = category;
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
