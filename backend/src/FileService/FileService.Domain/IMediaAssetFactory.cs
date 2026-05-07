using CSharpFunctionalExtensions;
using Shared;

namespace FileService.Domain;

public interface IMediaAssetFactory
{
    Result<VideoAsset, Error> CreateVideoForUpload(MediaData mediaData, MediaOwner owner);

    Result<PreviewAsset, Error> CreatePreviewForUpload(MediaData mediaData, MediaOwner owner);
}
