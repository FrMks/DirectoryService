using System.Text.Json;
using Shared;

namespace DirectoryService.Application.Exceptions;

// 404 - не нашлось что-то в БД
public class NotFoundException : Exception
{
    protected NotFoundException(Error[] errors)
        : base(JsonSerializer.Serialize(errors))
    {
    }
}