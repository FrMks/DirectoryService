using CSharpFunctionalExtensions;
using FileService.Domain.Entities.MediaAssetEntity;
using FileService.Domain.Enums;
using FileService.Domain.Enums.AssetTypeEnum;
using FileService.Domain.ValueObjects;
using Shared;

namespace FileService.Domain.Entities;

/// <summary>
/// upload original video -> mark uploaded -> process/convert video -> mark ready
/// </summary>
public class VideoAsset : MediaAsset
{
    private VideoAsset()
        : base() { }

    private VideoAsset(
        Guid id,
        MediaData mediaData,
        MediaStatus status,
        MediaOwner owner,
        StorageKey rawKey,
        StorageKey finalKey,
        StorageKey hlsRootKey,
        HlsResult hlsResult)
            : base(
                id,
                mediaData,
                status,
                AssetType.VIDEO,
                owner,
                rawKey,
                finalKey)
    {
        HlsRootKey = hlsRootKey;
        HlsResult = hlsResult;
    }

    public const long MAX_SIZE = 5_368_709_120;

    public const string BUCKET = "videos";
    public const string RAW_PREFIX = "raw";
    public const string HLS_PREFIX = "hls";
    public const string MASTER_PLAYLIST_NAME = "master.m3u8";

    public static readonly string[] AllowedExtensions = ["mp4", "mkv", "avi", "mov"];

    public StorageKey HlsRootKey { get; init; } // videos/hls/{video-id}

    public HlsResult HlsResult { get; private set; } = null!;

    public VideoMetadata? Metadata { get; private set; }

    public static UnitResult<Error> ValidateForUpload(MediaData mediaData)
    {
        if (!AllowedExtensions.Contains(mediaData.FileName.Extension))
        {
            return Error.Validation(
                "video.invalid.extension",
                $"File extension must be one of: {string.Join(", ", AllowedExtensions)}");
        }

        if (mediaData.ContentType.Category != MediaType.VIDEO)
        {
            return Error.Validation(
                "video.invalid.content-type",
                "File content type must be video");
        }

        if (mediaData.Size > MAX_SIZE)
        {
            return Error.Validation(
                "video.invalid.size",
                $"Failed size must be less than {MAX_SIZE} bytes");
        }

        return UnitResult.Success<Error>();
    }

    public static Result<VideoAsset, Error> CreateForUpload(Guid id, MediaData mediaData, MediaOwner owner)
    {
        UnitResult<Error> validationResult = ValidateForUpload(mediaData);
        if (validationResult.IsFailure)
            return validationResult.Error;

        Result<StorageKey, Error> rawKey = StorageKey.Create(BUCKET, RAW_PREFIX, id.ToString());
        if (rawKey.IsFailure)
            return rawKey.Error;

        Result<StorageKey, Error> hlsRootKey = StorageKey.Create(BUCKET, HLS_PREFIX, id.ToString());
        if (hlsRootKey.IsFailure)
            return hlsRootKey.Error;

        Result<StorageKey, Error> manifestKey = hlsRootKey.Value.AppendSegment(MASTER_PLAYLIST_NAME);
        if (manifestKey.IsFailure)
            return manifestKey.Error;

        return new VideoAsset(
            id,
            mediaData,
            MediaStatus.UPLOADING,
            owner,
            rawKey.Value,
            StorageKey.None,
            hlsRootKey.Value,
            new HlsResult(manifestKey.Value));
    }

    public override bool RequiresProcessing() => true;

    public UnitResult<Error> SetMetadata(VideoMetadata metadata)
    {
        if (metadata is null)
            return Error.Validation("video.metadata.required", "Video metadata is required");

        Metadata = metadata;
        UpdatedAt = DateTime.UtcNow;

        return UnitResult.Success<Error>();
    }

    #region Status

    public UnitResult<Error> CompleteProcessing(DateTime timestamp)
    {
        // videos/hls/{video.id}/master.m3u8
        return MarkReady(HlsResult.ManifestKey, timestamp);
    }

    public UnitResult<Error> MarkPendingProcessing(DateTime timestamp)
    {
        return ChangeStatus(MediaStatus.PENDING_PROCESSING, timestamp);
    }

    public UnitResult<Error> StartProcessing(DateTime timestamp)
    {
        return ChangeStatus(MediaStatus.PROCESSING, timestamp);
    }

    public UnitResult<Error> FailProcessing(DateTime timestamp)
    {
        return ChangeStatus(MediaStatus.FAILED, timestamp);
    }

    #endregion

    protected override bool CanChangeStatusTo(MediaStatus target)
    {
        return Status switch
        {
            // Если текущий, то можно перейти в => ....
            MediaStatus.UPLOADING => target is MediaStatus.UPLOADED or MediaStatus.FAILED or MediaStatus.DELETED,
            MediaStatus.UPLOADED => target is MediaStatus.PENDING_PROCESSING or MediaStatus.FAILED or MediaStatus.DELETED,
            MediaStatus.PENDING_PROCESSING => target is MediaStatus.PROCESSING or MediaStatus.FAILED or MediaStatus.DELETED,
            MediaStatus.PROCESSING => target is MediaStatus.READY or MediaStatus.FAILED or MediaStatus.DELETED,
            MediaStatus.READY => target == MediaStatus.DELETED,
            MediaStatus.FAILED => target == MediaStatus.DELETED,
            MediaStatus.DELETED => false,
            _ => false,
        };
    }
}
