namespace FileService.Web.EventRouting;

/*
File Service
  отправляет FileReady
  в exchange: file-events
  с routing key: file.ready.lesson
                ↓
RabbitMQ сопоставляет key с правилами очередей
                ↓
очередь Lesson Service получает событие
*/
public static class FileEventsRouting
{
    // Exchange это маршрутизатор сообщений в RabbitMQ, который определяет, как сообщения будут направляться к соответствующим очередям.
    public const string ExchangeName = "file-events";

    public static string FileReady(string targetEntityType)
    {
        var normalizedTargetEntityType = NormalizeTargetEntityType(targetEntityType);

        return $"file.ready.{normalizedTargetEntityType}";
    }

    public static string FileDeleted(string targetEntityType)
    {
        var normalizedTargetEntityType = NormalizeTargetEntityType(targetEntityType);

        return $"file.deleted.{normalizedTargetEntityType}";
    }

    public static string VideoProcessingCompleted(string targetEntityType)
    {
        var normalizedTargetEntityType = NormalizeTargetEntityType(targetEntityType);

        return $"video.processing.completed.{normalizedTargetEntityType}";
    }

    public static string VideoProcessingFailed(string targetEntityType)
    {
        var normalizedTargetEntityType = NormalizeTargetEntityType(targetEntityType);

        return $"video.processing.failed.{normalizedTargetEntityType}";
    }

    private static string NormalizeTargetEntityType(string targetEntityType)
    {
        if (string.IsNullOrWhiteSpace(targetEntityType))
        {
            throw new ArgumentException(
                "Target entity type cannot be null or whitespace.",
                nameof(targetEntityType));
        }

        return targetEntityType.ToLowerInvariant().Trim();
    }
}