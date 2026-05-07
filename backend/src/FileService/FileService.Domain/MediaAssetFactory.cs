using CSharpFunctionalExtensions;
using Shared;

namespace FileService.Domain;

public sealed class MediaAssetFactory : IMediaAssetFactory
{
    public Result<VideoAsset, Error> CreateVideoForUpload(MediaData mediaData, MediaOwner owner)
    {
        return VideoAsset.CreateForUpload(Guid.NewGuid(), mediaData, owner);
    }

    public Result<PreviewAsset, Error> CreatePreviewForUpload(MediaData mediaData, MediaOwner owner)
    {
        return PreviewAsset.CreateForUpload(Guid.NewGuid(), mediaData, owner);
    }
}
