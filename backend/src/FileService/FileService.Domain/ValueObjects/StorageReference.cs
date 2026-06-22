using System.Runtime.CompilerServices;
using CSharpFunctionalExtensions;
using Shared;

namespace FileService.Domain.ValueObjects;

public record StorageReference
{
    public StorageKey Key { get; init; } = null!;

    public long SizeBytes { get; init; }

    public string ContentType { get; init; } = string.Empty;

    public string? ETag { get; init; }

    private StorageReference() { }

    private StorageReference(
        StorageKey key,
        long sizeBytes,
        string contentType,
        string? eTag)
    {
        Key = key;
        SizeBytes = sizeBytes;
        ContentType = contentType;
        ETag = eTag;
    }

    public static Result<StorageReference, Error> Create(
        StorageKey key,
        long sizeBytes,
        string contentType,
        string? eTag)
    {
        if (key == StorageKey.None)
            return Error.Validation("storage.key.invalid", "Storage key is required");

        if (sizeBytes <= 0)
            return Error.Validation("storage.size.invalid", "Storage size must be greater than zero");

        if (string.IsNullOrWhiteSpace(contentType))
            return Error.Validation("storage.content-type.invalid", "Storage content type is required");

        return new StorageReference(
            key,
            sizeBytes,
            contentType.Trim(),
            string.IsNullOrWhiteSpace(eTag) ? null : eTag.Trim());
    }
}