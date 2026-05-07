namespace FileService.Domain;

/// <summary>
/// Жизненный цикл файла
/// </summary>
public enum MediaStatus
{
    UPLOADING,
    UPLOADED,
    READY,
    FAILED,
    DELETED,
}