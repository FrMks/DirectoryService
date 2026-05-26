using CSharpFunctionalExtensions;
using Shared;

namespace FileService.Domain;

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
        StorageKey hlsRootKey)
            : base(id, mediaData, status, AssetType.VIDEO, owner, rawKey, finalKey)
    {
        HlsRootKey = hlsRootKey;
    }

    public const long MAX_SIZE = 5_368_709_120;

    public const string BUCKET = "videos";
    public const string RAW_PREFIX = "raw";
    public const string HLS_PREFIX = "hls";
    public const string MASTER_PLAYLIST_NAME = "master.m3u8";

    public static readonly string[] AllowedExtensions = ["mp4", "mkv", "avi", "mov"];

    public StorageKey HlsRootKey { get; init; }

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

        return new VideoAsset(
            id,
            mediaData,
            MediaStatus.UPLOADING,
            owner,
            rawKey.Value,
            StorageKey.None,
            hlsRootKey.Value);
    }

    public UnitResult<Error> CompleteProcessing(DateTime timestamp)
    {
        Result<StorageKey, Error> finalKey = HlsRootKey.AppendSegment(MASTER_PLAYLIST_NAME);
        if (finalKey.IsFailure)
            return finalKey.Error;

        return MarkReady(finalKey.Value, timestamp);
    }
}
