using FileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Postgres.Configurations;

public class VideoAssetConfiguration : IEntityTypeConfiguration<VideoAsset>
{
    public void Configure(EntityTypeBuilder<VideoAsset> builder)
    {
        builder.OwnsOne(v => v.HlsRootKey, mb =>
        {
            mb.Property(k => k.Bucket).HasColumnName("hls_root_key_bucket");
            mb.Property(k => k.Prefix).HasColumnName("hls_root_key_prefix");
            mb.Property(k => k.Key).HasColumnName("hls_root_key_key");
            mb.Property(k => k.Value).HasColumnName("hls_root_key_value");
            mb.Property(k => k.FullPath).HasColumnName("hls_root_key_full_path");
        });

        builder.OwnsOne(v => v.Metadata, mb =>
        {
            mb.Property(m => m.Codec).HasColumnName("metadata_codec");
            mb.Property(m => m.Container).HasColumnName("metadata_container");
            mb.Property(m => m.Duration).HasColumnName("metadata_duration");
            mb.Property(m => m.Height).HasColumnName("metadata_height");
            mb.Property(m => m.Width).HasColumnName("metadata_width");
        });
    }
}