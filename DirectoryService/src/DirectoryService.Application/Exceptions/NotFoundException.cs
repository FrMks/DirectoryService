namespace DirectoryService.Application.Exceptions;

// 404 - не нашлось что-то в БД
public class NotFoundException : Exception
{
    protected NotFoundException(string record, Guid id)
        : base($"{record} with id {id} not found")
    {
    }
}