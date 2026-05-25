using System.Text.Json;
using FileService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Postgres.Configurations
{
    public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
    {
        public void Configure(EntityTypeBuilder<MediaAsset> builder)
        {
            builder.ToTable("media_assets");
            builder.HasKey(x => x.Id);

            builder.HasDiscriminator<string>("asset_type")
                .HasValue<VideoAsset>("video")
                .HasValue<PreviewAsset>("preview");

            builder.OwnsOne(m => m.MediaData, mb =>
            {
                mb.ToJson("media_data");

                mb.OwnsOne(md => md.ContentType, cb =>
                {
                    cb.Property(x => x.Category).HasConversion<string>().HasColumnName("category");
                    cb.Property(x => x.Value).HasColumnName("value");
                });

                mb.OwnsOne(md => md.FileName, fb =>
                {
                    fb.Property(x => x.Extension).HasColumnName("extension");
                    fb.Property(x => x.Name).HasColumnName("value");
                });

                mb.Property(md => md.Size).HasColumnName("size");
                mb.Property(md => md.ExpectedChunksCount).HasColumnName("expected_chunks_count");
            });

            builder.Property(x => x.Id).HasColumnName("id");

            builder.Property(x => x.Status).HasConversion<string>().HasColumnName("status");

            builder.Property(x => x.AssetType).HasConversion<string>().HasColumnName("asset_type");

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.Property(x => x.RawKey)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<StorageKey>(v, (JsonSerializerOptions?)null)!)
                .HasColumnName("raw_key")
                .HasColumnType("jsonb");

            builder.HasIndex(x => new
            {
                x.Status,
                x.CreatedAt,
            })
        }
    }
}