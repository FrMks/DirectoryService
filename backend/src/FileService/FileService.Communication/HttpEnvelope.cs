using Shared;

namespace FileService.Communication;

internal sealed record HttpEnvelope<TResponse>(
    TResponse? Result,
    IReadOnlyList<HttpError>? Errors,
    IReadOnlyList<HttpError>? ErrorList)
    where TResponse : class
{
    public Shared.Errors? GetErrors()
    {
        IReadOnlyList<HttpError>? responseErrors = Errors ?? ErrorList;
        if (responseErrors is null || responseErrors.Count == 0)
            return null;

        List<Error> errors = responseErrors
            .Select(error => error.ToError())
            .ToList();

        return errors;
    }
}

internal sealed record HttpError(
    string Code,
    string Message,
    ErrorType Type,
    string? InvalidField)
{
    public Error ToError() => Type switch
    {
        ErrorType.NotFound => Error.NotFound(Code, Message),
        ErrorType.Validation => Error.Validation(Code, Message, InvalidField),
        ErrorType.Conflict => Error.Conflict(Code, Message),
        _ => Error.Failure(Code, Message),
    };
}
