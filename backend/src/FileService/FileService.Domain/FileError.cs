using Shared;

namespace FileService.Infrastructure.S3;

public static class FileError
{
    public static Error BucketNotFound()
    {
        return Error.NotFound("no.such.bucket", "Bucket was not found");
    }

    public static Error UploadNotFound()
    {
        return Error.NotFound("no.such.upload", "Upload was not found");
    }

    public static Error ObjectNotFound(string? objectKey = null)
    {
        string key = objectKey ?? string.Empty;
        return Error.NotFound("no.such.object", $"Object with key {key} was not found");
    }

    public static Error Forbidden()
    {
        return Error.Failure("access.defied", "Access to the resource is denied");
    }

    public static Error ValidationFailed()
    {
        string message = "Request store failed validation";

        return Error.Failure("validation.failed", $"Validation failed. {message}");
    }

    public static Error InternalServerError()
    {
        return Error.Failure("internal.server.error", "An internal server error occurred");
    }

    public static Error OperationCancelled()
    {
        return Error.Failure("operation.cancelled", "The operation was cancelled");
    }

    public static Error NetworkIssue()
    {
        return Error.Failure("network.issue", "A network error occurred while processing the request");
    }

    public static Error Unknown()
    {
        return Error.Failure("unknown.error", "An unknown error occurred");
    }
}