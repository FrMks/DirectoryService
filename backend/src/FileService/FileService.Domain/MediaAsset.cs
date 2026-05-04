using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using CSharpFunctionalExtensions;
using Shared;

namespace FileService.Domain;

public abstract class MediaAsset
{
    /// <summary>
    /// Unique identifier of the media asset.
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// Data and metadata associated with the media asset.
    /// </summary>
    public MediaData MediaData { get; protected set; } = null!;

    /// <summary>
    /// The type of the asset (e.g., Image, Video).
    /// </summary>
    public AssetType AssetType { get; protected set; }

    /// <summary>
    /// Date and time when the asset was created in UTC.
    /// </summary>
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time when the asset was last updated in UTC.
    /// </summary>
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// The storage key representing the physical location of the asset.
    /// </summary>
    public StorageKey Key { get; protected set; } = null!;

    public MediaOwner Owner { get; protected set; } = null!;

    public MediaStatus Status { get; protected set; }

    protected MediaAsset(
        Guid id,
        MediaData mediaData,
        MediaStatus status,
        AssetType assetType,
        MediaOwner owner,
        StorageKey key)
    {
        Id = id;
        MediaData = mediaData;
        Status = status;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
        AssetType = assetType;
        Owner = owner;
        Key = key;
    }
}
