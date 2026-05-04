using System.Reflection;
using System.Runtime.InteropServices;
using CSharpFunctionalExtensions;
using Shared;

namespace FileService.Domain;

public abstract class MediaAsset
{
    /// <summary>
    /// Unique identifier of the media asset.
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// Data and metadata associated with the media asset.
    /// </summary>
    public MediaData MediaData { get; protected set; }

    /// <summary>
    /// The type of the asset (e.g., Image, Video).
    /// </summary>
    public AssetType AssetType { get; protected set; }

    /// <summary>
    /// Date and time when the asset was created in UTC.
    /// </summary>
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time when the asset was last updated in UTC.
    /// </summary>
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// The storage key representing the physical location of the asset.
    /// </summary>
    public StorageKey RawKey { get; protected set; }
}

public sealed record StorageKey
{
    /// <summary>
    /// The unique key or name of the file.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// The path prefix or folder structure.
    /// </summary>
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

    /// <summary>
    /// Normalizes a path prefix by trimming whitespace, replacing backslashes with forward slashes, 
    /// and removing empty segments.
    /// </summary>
    /// <example>
    /// "folder/subfolder" -> "folder/subfolder"
    /// "folder\\subfolder" -> "folder/subfolder"
    /// " /folder///subfolder/ " -> "folder/subfolder"
    /// null or empty -> ""
    /// </example>
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

    /// <summary>
    /// Normalizes a single path segment by trimming whitespace. 
    /// Returns an error if the segment contains path separators.
    /// </summary>
    /// <example>
    /// "file.txt" -> "file.txt"
    /// "  folder  " -> "folder"
    /// "path/to" -> Error (contains slash)
    /// </example>
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