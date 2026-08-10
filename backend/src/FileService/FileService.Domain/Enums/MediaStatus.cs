namespace FileService.Domain.Enums;

/// <summary>
/// Жизненный цикл файла
/// </summary>
public enum MediaStatus
{
    UPLOADING,
    UPLOADED,
    PENDING_PROCESSING,
    PROCESSING,

    /// <summary>
    /// Файл полностью загружен, проверен, сохранен, и может использоваться клиентом.
    /// </summary>
    READY,
    FAILED,
    DELETED,
}