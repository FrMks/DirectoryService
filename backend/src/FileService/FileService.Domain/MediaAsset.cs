using System.Reflection;
using System.Runtime.InteropServices;
using CSharpFunctionalExtensions;
using Shared;

namespace FileService.Domain;

public abstract class MediaAsset
{
    public Guid Id { get; protected set; }

    public MediaData MediaData { get; protected set; }

    public AssetType AssetType { get; protected set; }

    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;

    public StorageKey RawKey { get; protected set; }
}

public sealed record StorageKey
{
    public string Key { get; }

    public string Prefix { get; }

    /// <summary>
    /// Like backet.
    /// </summary>
    public string Location { get; }

    /// <summary>
    /// Only key
    /// Key = Prefix + Key.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// FullPath (plus Location)
    /// </summary>
    public string FullPath { get; }

    private StorageKey(string location, string prefix, string key)
    {
        Location = location;
        Prefix = prefix;
        Key = key;
        Value = string.IsNullOrWhiteSpace(Prefix) ? Key : $"{Prefix}/{Key}";
        FullPath = $"{Location}/{Value}";
    }

    public static Result<StorageKey, Error> Create(string location, string? prefix, string key)
    {
        if (string.IsNullOrWhiteSpace(location))
            return Error.Validation(null, "location is invalid");

        Result<string, Error> normalizedKeyResult = NormalizeSegment(key);
        if (normalizedKeyResult.IsFailure)
            return normalizedKeyResult.Error;

        Result<string, Error> normalizedPrefixResult = NormalizeSegment(prefix);
        if (normalizedPrefixResult.IsFailure)
            return normalizedPrefixResult.Error;

        return new StorageKey(location.Trim(), normalizedKeyResult.Value, normalizedKeyResult.Value);
    }

    private static Result<string, Error> NormalizePrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return string.Empty;

        string[] parts = prefix.Trim().Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        List<string> normalizedParts = [];
        foreach (string part in parts)
        {
            Result<string, Error> normalizedPart = NormalizeSegment(part);
            if (normalizedPart.IsFailure)
                return normalizedPart;

            if (!string.IsNullOrEmpty(normalizedPart.Value))
                normalizedParts.Add(normalizedPart.Value);
        }

        return string.Join('/', normalizedParts);
    }

    private static Result<string, Error> NormalizeSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation(null, "key");

        string trimmed = value.Trim();

        if (trimmed.Contains('/', StringComparison.Ordinal) || trimmed.Contains('\\', StringComparison.Ordinal))
            return Error.Validation(null, "key");

        return trimmed;
    }
}