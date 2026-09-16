using System.Runtime.Serialization;

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
    READY,
    FAILED,
    DELETED
}