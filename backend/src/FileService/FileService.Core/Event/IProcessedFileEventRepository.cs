namespace FileService.Core.Event;

public interface IProcessedFileEventRepository
{
    // True - событие новое, его можно обрабатывать
    // False - такое событие уже было обработано, повторную обработку выполнять нельзя
    Task<bool> TryRegisterAsync(Guid eventId, string eventType, CancellationToken cancellationToken);
}