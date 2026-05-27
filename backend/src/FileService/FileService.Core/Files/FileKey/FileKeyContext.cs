namespace FileService.Core.Files.FileKey;

public sealed record FileKeyContext
(
    string OriginalFileName,
    string ContentType,
    Guid? OwnerId = null
);