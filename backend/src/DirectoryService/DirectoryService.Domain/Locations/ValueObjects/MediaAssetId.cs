namespace DirectoryService.Domain.Locations.ValueObjects;

public record MediaAssetId
{
    private MediaAssetId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    public static MediaAssetId Empty() => new(Guid.Empty);

    public static MediaAssetId FromValue(Guid value) => new(value);

    public static implicit operator Guid(MediaAssetId mediaAssetId) => mediaAssetId.Value;
}