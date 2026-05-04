using CSharpFunctionalExtensions;
using Shared;

namespace FileService.Domain;

public class PreviewAsset : MediaAsset
{
    private PreviewAsset(
        Guid id,
        MediaData mediaData,
        MediaStatus status,
        MediaOwner owner)
            : base(id, mediaData, status, AssetType.PREVIEW, owner)
    {
    }

    public const string ALLOWED_CONTENT_TYPE = "image";
    public const long MAX_SIZE = 10_485_760; // 10 MB
    public const string BUCKET = "preview";
    public const string RAW_PREFIX = "raw";
    public static readonly string[] AllowedExtensions = ["jpg", "jpeg", "png", "webp"];

    public static UnitResult<Error> Validate(MediaData mediaData)
    {
        if (!AllowedExtensions.Contains(mediaData.FileName.Extension))
        {
            return Error.Validation(
                "preview.invalid.extension",
                $"File extension must be one of: {string.Join(", ", AllowedExtensions)}");
        }

        if (mediaData.ContentType.Category != MediaType.IMAGE)
        {
            return Error.Validation(
                "preview.invalid.content-type",
                $"File content type must be {ALLOWED_CONTENT_TYPE}");
        }

        if (mediaData.Size > MAX_SIZE)
        {
            return Error.Validation(
                "preview.invalid.size",
                $"Failed size must be less than {MAX_SIZE} bytes");
        }

        return UnitResult.Success<Error>();
    }

    public static Result<PreviewAsset, Error> CreateForUpload(Guid id, MediaData mediaData, MediaOwner owner)
    {
        UnitResult<Error> validationResult = Validate(mediaData);
        if (validationResult.IsFailure)
            return validationResult.Error;

        return new PreviewAsset(
            id,
            mediaData,
            MediaStatus.UPLOADING,
            owner);
    }
}
