using Amazon.S3;
using FileService.Domain.Errors;
using System.Net;
using Shared;

namespace FileService.Infrastructure.S3;

public static class S3ErrorMapper
{
    public static Error ToError(Exception ex) => ex switch
    {
        AmazonS3Exception { StatusCode: HttpStatusCode.NotFound }
            => FileError.ObjectNotFound(),

        AmazonS3Exception { ErrorCode: "NoSuchBucket" }
            => FileError.BucketNotFound(),

        AmazonS3Exception { ErrorCode: "AccessDenied" or "SignatureDoesNotMatch" or "InvalidAccessKeyId" }
            => FileError.Forbidden(),

        AmazonS3Exception { ErrorCode: "InvalidRequest" or "InvalidArgument" }
            => FileError.ValidationFailed(),

        AmazonS3Exception { ErrorCode: "InternalError" }
            => FileError.InternalServerError(),

        AmazonS3Exception { ErrorCode: "NoSuchKey" }
            => FileError.ObjectNotFound(),

        AmazonS3Exception { ErrorCode: "NoSuchUpload" }
            => FileError.UploadNotFound(),

        ArgumentException
            => FileError.ValidationFailed(),

        HttpRequestException
            => FileError.NetworkIssue(),

        OperationCanceledException
            => FileError.OperationCancelled(),

        _ => FileError.Unknown(),
    };
}
