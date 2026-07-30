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

        builder.OwnsOne(va => va.HlsResult, hb =>
        {
            hb.OwnsOne(h => h.ManifestKey, mk =>
            {
                mk.Property(k => k.Bucket).HasColumnName("hls_result_manifest_key_bucket");
                mk.Property(k => k.Prefix).HasColumnName("hls_result_manifest_key_prefix");
                mk.Property(k => k.Key).HasColumnName("hls_result_manifest_key_key");
                mk.Property(k => k.Value).HasColumnName("hls_result_manifest_key_value");
                mk.Property(k => k.FullPath).HasColumnName("hls_result_manifest_key_full_path");
            });
        });

        builder.OwnsOne(v => v.Metadata, mb =>
        {
            mb.Property(m => m.Codec).HasColumnName("metadata_codec");
            mb.Property(m => m.Container).HasColumnName("metadata_container");
            mb.Property(m => m.Duration).HasColumnName("metadata_duration");
            mb.Property(m => m.Height).HasColumnName("metadata_height");
            mb.Property(m => m.Width).HasColumnName("metadata_width");
        });

        builder.OwnsOne(va => va.PreviewKey, pb =>
        {
            pb.Property(k => k.Bucket).HasColumnName("preview_key_bucket");
            pb.Property(k => k.Prefix).HasColumnName("preview_key_prefix");
            pb.Property(k => k.Key).HasColumnName("preview_key_key");
            pb.Property(k => k.Value).HasColumnName("preview_key_value");
            pb.Property(k => k.FullPath).HasColumnName("preview_key_full_path");
        });
    }
}