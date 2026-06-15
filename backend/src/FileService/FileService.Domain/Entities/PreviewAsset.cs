using CSharpFunctionalExtensions;
using FileService.Domain.Entities.MediaAssetEntity;
using FileService.Domain.Enums;
using FileService.Domain.Enums.AssetTypeEnum;
using FileService.Domain.ValueObjects;
using Shared;

namespace FileService.Domain.Entities;

/// <summary>
/// Lifecycle: upload image => mark uploaded => mark ready
/// </summary>
public class PreviewAsset : MediaAsset
{
    private PreviewAsset()
        : base() { }

    private PreviewAsset(
        Guid id,
        MediaData mediaData,
        MediaStatus status,
        // MediaOwner owner,
        StorageKey rawKey,
        StorageKey finalKey)
            : base(
                id,
                mediaData,
                status,
                AssetType.PREVIEW,
                // owner,
                rawKey,
                finalKey)
    {
    }

    public const long MAX_SIZE = 10_485_760;

    public const string BUCKET = "preview";
    public const string RAW_PREFIX = "raw";

    public static readonly string[] AllowedExtensions = ["jpg", "jpeg", "png", "webp"];

    public static UnitResult<Error> ValidateForUpload(MediaData mediaData)
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
                "File content type must be image");
        }

        if (mediaData.Size > MAX_SIZE)
        {
            return Error.Validation(
                "preview.invalid.size",
                $"Failed size must be less than {MAX_SIZE} bytes");
        }

        return UnitResult.Success<Error>();
    }

    // public static Result<PreviewAsset, Error> CreateForUpload(Guid id, MediaData mediaData, MediaOwner owner)
    public static Result<PreviewAsset, Error> CreateForUpload(Guid id, MediaData mediaData)
    {
        UnitResult<Error> validationResult = ValidateForUpload(mediaData);
        if (validationResult.IsFailure)
            return validationResult.Error;

        Result<StorageKey, Error> rawKey = StorageKey.Create(BUCKET, RAW_PREFIX, id.ToString());
        if (rawKey.IsFailure)
            return rawKey.Error;

        return new PreviewAsset(
            id,
            mediaData,
            MediaStatus.UPLOADING,
            // owner,
            rawKey.Value,
            StorageKey.None);
    }

    public UnitResult<Error> CompleteUpload(DateTime timestamp)
    {
        UnitResult<Error> uploadedResult = MarkUploaded(timestamp);
        if (uploadedResult.IsFailure)
            return uploadedResult.Error;

        return MarkReady(RawKey, timestamp);
    }
}
