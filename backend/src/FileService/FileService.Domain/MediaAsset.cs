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
        // if (string.IsNullOrWhiteSpace())
    }
}