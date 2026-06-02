using CSharpFunctionalExtensions;
using Shared;

namespace FileService.Domain.ValueObjects;

/// <summary>
/// Bucket = "videos"
/// Prefix = "raw"
/// Key = "{video-id}"
/// Value = "raw/{video-id}"
/// FullPath = "videos/raw/{video-id}"
/// </summary>
public sealed record StorageKey
{
    public static StorageKey None { get; } = new(string.Empty, string.Empty, string.Empty);

    public string Bucket { get; init; }

    public string Prefix { get; init; }

    public string Key { get; init; }

    public string Value { get; init; }

    public string FullPath { get; init; }

    private StorageKey() { }

    private StorageKey(string bucket, string prefix, string key)
    {
        Bucket = bucket;
        Prefix = prefix;
        Key = key;
        Value = string.IsNullOrWhiteSpace(Prefix) ? Key : $"{Prefix}/{Key}";
        FullPath = string.IsNullOrWhiteSpace(Bucket) ? Value : $"{Bucket}/{Value}";
    }

    public static Result<StorageKey, Error> Create(string bucket, string? prefix, string key)
    {
        Result<string, Error> normalizedBucketResult = NormalizeSegment(bucket);
        if (normalizedBucketResult.IsFailure)
            return Error.Validation("storage.bucket.invalid", "Bucket is required");

        Result<string, Error> normalizedPrefixResult = NormalizePrefix(prefix);
        if (normalizedPrefixResult.IsFailure)
            return normalizedPrefixResult.Error;

        Result<string, Error> normalizedKeyResult = NormalizeSegment(key);
        if (normalizedKeyResult.IsFailure)
            return normalizedKeyResult.Error;

        return new StorageKey(normalizedBucketResult.Value, normalizedPrefixResult.Value, normalizedKeyResult.Value);
    }

    public Result<StorageKey, Error> AppendSegment(string segment)
    {
        Result<string, Error> normalizedSegment = NormalizeSegment(segment);
        if (normalizedSegment.IsFailure)
            return normalizedSegment.Error;

        string prefix = string.IsNullOrWhiteSpace(Prefix)
            ? Key
            : $"{Prefix}/{Key}";

        return new StorageKey(Bucket, prefix, normalizedSegment.Value);
    }

    private static Result<string, Error> NormalizePrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return string.Empty;

        string[] parts = prefix.Trim().Replace('\\', '/').Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        List<string> normalizedParts = [];
        foreach (string part in parts)
        {
            Result<string, Error> normalizedPart = NormalizeSegment(part);
            if (normalizedPart.IsFailure)
                return normalizedPart;

            normalizedParts.Add(normalizedPart.Value);
        }

        return string.Join('/', normalizedParts);
    }

    private static Result<string, Error> NormalizeSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation("storage.segment.invalid", "Path segment is required");

        string trimmed = value.Trim();

        if (trimmed.Contains('/', StringComparison.Ordinal) || trimmed.Contains('\\', StringComparison.Ordinal))
            return Error.Validation("storage.segment.invalid", "Path segment must not contain slashes");

        return trimmed;
    }
}
