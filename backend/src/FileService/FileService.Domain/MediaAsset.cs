using CSharpFunctionalExtensions;
using Shared;

namespace FileService.Domain;

public abstract class MediaAsset
{
    public Guid Id { get; protected set; }

    public MediaData MediaData { get; protected set; } = null!;

    public AssetType AssetType { get; protected set; }

    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;

    public StorageKey RawKey { get; protected set; } = null!;

    public StorageKey FinalKey { get; protected set; } = null!;

    public MediaOwner Owner { get; protected set; } = null!;

    public MediaStatus Status { get; protected set; }

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
}
