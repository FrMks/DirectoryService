using System.Reflection;
using System.Runtime.InteropServices;
using CSharpFunctionalExtensions;
using Shared;

namespace FileService.Domain;

public sealed record MediaData
{
    public FileName FileName { get; init; }

    public ContentType ContentType { get; init; }

    // Should be > 0
    public long Size { get; init; }

    // Should be > 0
    public int ExpectedChunksCount { get; init; }

    private MediaData() { }

    private MediaData(FileName fileName, ContentType contentType, long size, int expectedChunksCount)
    {
        FileName = fileName;
        ContentType = contentType;
        Size = size;
        ExpectedChunksCount = expectedChunksCount;
    }

    public static Result<MediaData, Error> Create(
        FileName fileName,
        ContentType contentType,
        long size,
        int expectedChunksCount)
    {
        if (size <= 0)
            return Error.Validation(null, "Size should be greater 0");

        if (expectedChunksCount <= 0)
            return Error.Validation(null, "Expected chunks count should be greater 0");

        return new MediaData(fileName, contentType, size, expectedChunksCount);
    }
}
