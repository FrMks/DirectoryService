using CSharpFunctionalExtensions;
using Shared;

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

    public static Result<LocationPreviewMetadata, Error> Create(
        MediaAssetId assetId,
        string fileName,
        string contentType,
        long size,
        DateTime attachedAt,
        DateTime lastVerifiedAt)
    {
        if (assetId is null)
            return Error.Validation("location.preview.asset-id.required", "Asset id is required.");

        if (string.IsNullOrWhiteSpace(fileName))
            return Error.Validation("location.preview.file-name.required", "File name is required.");

        if (string.IsNullOrWhiteSpace(contentType))
            return Error.Validation("location.preview.content-type.required", "Content type is required.");

        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return Error.Validation("location.preview.content-type.invalid", "Location preview must be an image.");

        if (size <= 0)
            return Error.Validation("location.preview.size.invalid", "File size must be greater than zero.");

        if (attachedAt == default)
            return Error.Validation("location.preview.attached-at.required", "Attached date is required.");

        if (lastVerifiedAt == default)
            return Error.Validation("location.preview.last-verified-at.required", "Last verified date is required.");

        return Result.Success<LocationPreviewMetadata, Error>(new LocationPreviewMetadata(
            assetId,
            fileName.Trim(),
            contentType.Trim(),
            size,
            attachedAt,
            lastVerifiedAt));
    }
}
