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
    }
}