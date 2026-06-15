using CSharpFunctionalExtensions;
using FileService.Domain.ValueObjects;
using Shared;

namespace FileService.Domain.Entities.MediaAssetEntity;

public sealed class MediaAssetFactory : IMediaAssetFactory
{
    public Result<VideoAsset, Error> CreateVideoForUpload(MediaData mediaData, MediaOwner owner)
    {
        return VideoAsset.CreateForUpload(Guid.NewGuid(), mediaData);
    }

    public Result<PreviewAsset, Error> CreatePreviewForUpload(MediaData mediaData, MediaOwner owner)
    {
        return PreviewAsset.CreateForUpload(Guid.NewGuid(), mediaData);
    }
}
