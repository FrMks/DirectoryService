using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileService.Domain;

public class VideoAsset : MediaAsset
{
    public const string LOCATION = "videos";
    public const string RAW_PREFIX = "raw";
    public const string ALLOWED_CONTENT_TYPE = "video";

    public static readonly string[] AllowedExtensions = ["mp4", "mkv", "avi", "mov"];

    public VideoAsset(
        Guid id,
        MediaData mediaData,
        MediaStatus status,
        AssetType assetType,
        MediaOwner owner)
            : base(id, mediaData, status, assetType, owner)
    {
    }
}