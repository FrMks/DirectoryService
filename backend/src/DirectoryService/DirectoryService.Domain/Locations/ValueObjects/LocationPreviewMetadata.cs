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
            return Result.Failure<LocationPreviewMetadata, Error>("Asset id is required.");

        if (string.IsNullOrWhiteSpace(fileName))
            return Result.Failure<LocationPreviewMetadata, Error>("File name is required.");

        if (string.IsNullOrWhiteSpace(contentType))
            return Result.Failure<LocationPreviewMetadata, Error>("Content type is required.");

        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return Result.Failure<LocationPreviewMetadata, Error>("Location preview must be an image.");

        if (size <= 0)
            return Result.Failure<LocationPreviewMetadata, Error>("File size must be greater than zero.");

        if (attachedAt == default)
            return Result.Failure<LocationPreviewMetadata, Error>("Attached date is required.");

        if (lastVerifiedAt == default)
            return Result.Failure<LocationPreviewMetadata, Error>("Last verified date is required.");

        return Result.Success<LocationPreviewMetadata, Error>(new LocationPreviewMetadata(
            assetId,
            fileName.Trim(),
            contentType.Trim(),
            size,
            attachedAt,
            lastVerifiedAt));
    }
}