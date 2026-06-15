using CSharpFunctionalExtensions;
using FileService.Domain.ValueObjects;
using Shared;

namespace FileService.Domain.Entities.MediaAssetEntity;

public interface IMediaAssetFactory
{
    Result<VideoAsset, Error> CreateVideoForUpload(MediaData mediaData, MediaOwner owner);

    Result<PreviewAsset, Error> CreatePreviewForUpload(MediaData mediaData, MediaOwner owner);
}
