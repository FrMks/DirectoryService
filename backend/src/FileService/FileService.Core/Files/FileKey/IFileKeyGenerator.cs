namespace FileService.Core.Files.FileKey;

public interface IFileKeyGenerator
{
    string GenerateRawFileKey(FileKeyContext context);
}