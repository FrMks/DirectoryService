namespace FileService.Contracts;

public record VideoProcessingStatusResponse
(
    Guid AssetId,
    string Status,
    string? CurrentStep,
    int Percent,
    string? ErrorCode
);