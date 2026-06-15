using System.Text.Json;
using FileService.Domain.Entities;
using FileService.Domain.Entities.MediaAssetEntity;
using FileService.Domain.Enums.AssetTypeEnum;
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

            builder.HasDiscriminator(x => x.AssetType)
                .HasValue<VideoAsset>(AssetType.VIDEO)
                .HasValue<PreviewAsset>(AssetType.PREVIEW);

            builder.OwnsOne(m => m.MediaData, mb =>
            {
                mb.ToJson("media_data");

                mb.OwnsOne(md => md.ContentType, cb =>
                {
                    cb.Property(x => x.Category).HasConversion<string>();
                    cb.Property(x => x.Value);
                });

                mb.OwnsOne(md => md.FileName, fb =>
                {
                    fb.Property(x => x.Extension);
                    fb.Property(x => x.Name);
                });

                mb.Property(md => md.Size);
                mb.Property(md => md.ExpectedChunksCount);
            });

            builder.Property(x => x.Id).HasColumnName("id");

            builder.Property(x => x.Status).HasConversion<string>().HasColumnName("status");

            builder.Property(x => x.AssetType).HasConversion<string>().HasColumnName("asset_type");

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.OwnsOne(m => m.RawKey, mb =>
            {
                mb.Property(rk => rk.Bucket).HasColumnName("raw_key_bucket");
                mb.Property(rk => rk.Prefix).HasColumnName("raw_key_prefix");
                mb.Property(rk => rk.Key).HasColumnName("raw_key_key");
                mb.Property(rk => rk.Value).HasColumnName("raw_key_value");
                mb.Property(rk => rk.FullPath).HasColumnName("raw_key_full_path");
            });

            builder.OwnsOne(m => m.FinalKey, mb =>
            {
                mb.Property(rk => rk.Bucket).HasColumnName("final_key_bucket");
                mb.Property(rk => rk.Prefix).HasColumnName("final_key_prefix");
                mb.Property(rk => rk.Key).HasColumnName("final_key_key");
                mb.Property(rk => rk.Value).HasColumnName("final_key_value");
                mb.Property(rk => rk.FullPath).HasColumnName("final_key_full_path");
            });

            builder.OwnsOne(m => m.Owner, mb =>
            {
                mb.Property(o => o.Context).HasColumnName("owner_context").HasMaxLength(50);
                mb.Property(o => o.EntityId).HasColumnName("owner_entity_id");
            });

            builder.HasIndex(x => new
            {
                x.Status,
                x.CreatedAt,
            });
        }
    }
}