namespace FileService.Domain.Enums;

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