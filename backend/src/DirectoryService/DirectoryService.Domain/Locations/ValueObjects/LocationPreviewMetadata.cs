using CSharpFunctionalExtensions;

namespace DirectoryService.Domain.Locations.ValueObjects;

public sealed record LocationPreviewMetadata
{
    private LocationPreviewMetadata(
        MediaAssetId assetId,
        string fileName,
        string contentType,
        long size,
        DateTime attachedAt,
        DateTime lastVerifiedAt)
    {
        AssetId = assetId;
        FileName = fileName;
        ContentType = contentType;
        Size = size;
        AttachedAt = attachedAt;
        LastVerifiedAt = lastVerifiedAt;
    }

    public MediaAssetId AssetId { get; }

    public string FileName { get; }

    public string ContentType { get; }

    public long Size { get; }

    public DateTime AttachedAt { get; }

    public DateTime LastVerifiedAt { get; }

    public static Result<LocationPreviewMetadata> Create(
        MediaAssetId assetId,
        string fileName,
        string contentType,
        long size,
        DateTime attachedAt,
        DateTime lastVerifiedAt)
    {
        if (assetId is null)
            return Result.Failure<LocationPreviewMetadata>("Asset id is required.");

        if (string.IsNullOrWhiteSpace(fileName))
            return Result.Failure<LocationPreviewMetadata>("File name is required.");

        if (string.IsNullOrWhiteSpace(contentType))
            return Result.Failure<LocationPreviewMetadata>("Content type is required.");

        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return Result.Failure<LocationPreviewMetadata>("Location preview must be an image.");

        if (size <= 0)
            return Result.Failure<LocationPreviewMetadata>("File size must be greater than zero.");

        if (attachedAt == default)
            return Result.Failure<LocationPreviewMetadata>("Attached date is required.");

        if (lastVerifiedAt == default)
            return Result.Failure<LocationPreviewMetadata>("Last verified date is required.");

        return Result.Success(new LocationPreviewMetadata(
            assetId,
            fileName.Trim(),
            contentType.Trim(),
            size,
            attachedAt,
            lastVerifiedAt));
    }
}