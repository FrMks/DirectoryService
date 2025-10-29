using System.Text.Json;
using Shared;

namespace DirectoryService.Application.Exceptions;

public class BadRequestException : Exception
{
    // protected - могут воспользоваться конструктором только те, кто наследует класс
    protected BadRequestException(Error[] errors)
        : base(JsonSerializer.Serialize(errors))
    {
    }
}