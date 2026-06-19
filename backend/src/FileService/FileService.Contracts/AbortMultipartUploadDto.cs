namespace FileService.Contracts;

public record AbortMultipartUploadDto(
    Guid MediaAssetId,
    string UploadId
);