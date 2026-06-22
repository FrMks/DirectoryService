using CSharpFunctionalExtensions;
using FileService.Domain.Enums;
using FileService.Domain.Enums.AssetTypeEnum;
using FileService.Domain.ValueObjects;
using Shared;

namespace FileService.Domain.Entities.MediaAssetEntity;

public abstract class MediaAsset
{
    public Guid Id { get; protected set; }

    public MediaData MediaData { get; protected set; } = null!;

    /// <summary>
    /// AssetType says what role this file has in your business (videoAsset or previewAsset),rather than what kind of file it is (mp4, jpg и т.д.)
    /// </summary>
    public AssetType AssetType { get; protected set; }

    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// Путь к исходной версии videos/raw/{video-id} или preview/raw/{preview-id}
    /// </summary>
    public StorageKey RawKey { get; protected set; } = null!;

    /// <summary>
    /// Путь к финальной версии, который потом будет исопльзовать система
    /// Для видео финальная версия отличается от raw, потому что видео после загрузки конвертируется в HLS
    /// videos/hls/{video-id}/master.m3u8
    /// Для превью финальная версия такая же, как raw, потому что превью не требует обработки
    /// </summary>
    public StorageKey FinalKey { get; protected set; } = null!;

    public StorageReference? UploadedObject { get; protected set; }

    /// <summary>
    /// Who or what owns this media. Context="lesson", "course", "user", "department" + entityId
    /// </summary>
    public MediaOwner Owner { get; protected set; } = null!;

    public MediaStatus Status { get; protected set; }

    protected MediaAsset() { }

    protected MediaAsset(
        Guid id,
        MediaData mediaData,
        MediaStatus status,
        AssetType assetType,
        MediaOwner owner,
        StorageKey rawKey,
        StorageKey finalKey)
    {
        Id = id;
        MediaData = mediaData;
        Status = status;
        AssetType = assetType;
        Owner = owner;
        RawKey = rawKey;
        FinalKey = finalKey;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Result<MediaAsset, Error> CreateForUpload(
        MediaData mediaData,
        AssetType assetType,
        MediaOwner owner)
    {
        var assetId = Guid.NewGuid();

        switch (assetType)
        {
            case AssetType.VIDEO:
                var videoResult = VideoAsset.CreateForUpload(assetId, mediaData, owner);
                return videoResult.IsFailure ? videoResult.Error : videoResult.Value;
            case AssetType.PREVIEW:
                var previewResult = PreviewAsset.CreateForUpload(assetId, mediaData, owner);
                return previewResult.IsFailure ? previewResult.Error : previewResult.Value;
            default:
                throw new ArgumentOutOfRangeException(nameof(assetType), assetType, null);
        }
    }

    #region Status

    public UnitResult<Error> MarkUploaded(DateTime timestamp)
    {
        return ChangeStatus(MediaStatus.UPLOADED, timestamp);
    }

    public UnitResult<Error> MarkReady(StorageKey finalKey, DateTime timestamp)
    {
        if (Status == MediaStatus.READY)
            return UnitResult.Success<Error>();

        UnitResult<Error> result = ChangeStatus(MediaStatus.READY, timestamp);
        if (result.IsFailure)
            return result.Error;

        FinalKey = finalKey;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkFailed(DateTime timestamp)
    {
        return ChangeStatus(MediaStatus.FAILED, timestamp);
    }

    public UnitResult<Error> MarkDeleted(DateTime timestamp)
    {
        return ChangeStatus(MediaStatus.DELETED, timestamp);
    }

    private UnitResult<Error> ChangeStatus(MediaStatus target, DateTime timestamp)
    {
        if (Status == target)
            return UnitResult.Success<Error>();

        bool allowed = Status switch
        {
            MediaStatus.UPLOADING => target is MediaStatus.UPLOADED or MediaStatus.FAILED or MediaStatus.DELETED,
            MediaStatus.UPLOADED => target is MediaStatus.READY or MediaStatus.FAILED or MediaStatus.DELETED,
            MediaStatus.READY => target == MediaStatus.DELETED,
            MediaStatus.FAILED => target == MediaStatus.DELETED,
            MediaStatus.DELETED => false,
            _ => false,
        };

        if (!allowed)
        {
            return Error.Validation(
                "media.invalid.status-transition",
                $"Cannot change status from {Status} to {target}");
        }

        Status = target;
        UpdatedAt = timestamp;
        return UnitResult.Success<Error>();
    }

    #endregion

    public UnitResult<Error> AttachUploadedObject(StorageReference storageReference)
    {
        if (Status != MediaStatus.UPLOADING)
        {
            return Error.Validation(
                "media.invalid.status",
                $"Cannot attach uploaded object while media asset is in status {Status}");
        }

        UploadedObject = storageReference;
        UpdatedAt = DateTime.UtcNow;

        return UnitResult.Success<Error>();
    }
}
