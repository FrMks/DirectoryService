using FileService.Domain.MediaProcessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Postgres.Configurations;

public class VideoProcessingConfiguration : IEntityTypeConfiguration<VideoProcess>
{
    public void Configure(EntityTypeBuilder<VideoProcess> builder)
    {
        builder.ToTable("video_processing", "files");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.VideoAssetId).HasColumnName("video_asset_id");
        builder.Property(x => x.Status).HasConversion<string>().HasColumnName("status");
        builder.Property(x => x.ProgressPercentage).HasColumnName("progress_percentage");
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message");
        builder.Property(x => x.StartedAt).HasColumnName("started_at");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");
        builder.Property(x => x.IsCriticalError).HasColumnName("is_critical_error");
        builder.Property(x => x.RetryCount).HasColumnName("retry_count");
        builder.Property(x => x.MaxRetries).HasColumnName("max_retries");
        builder.Property(x => x.NextRetryAt).HasColumnName("next_retry_at");

        // Когда у нас идет речь у жестко связанные сущности надо использовать OwnsMany вместо HasMany
        // То есть шаг GENERATE_HLS сам по себе не существует. Он существует как часть конкретной обработки видео.
        // Если удалится родитель, то удалятся и дочерние сущности
        // Без родительской сущности не будет существовать дочерняя
        // У него отдельная таблицы
        builder.OwnsMany(vp => vp.Steps, sb =>
        { // У видео процесса есть связь с шагами этого процесса
            sb.ToTable("processing_steps", "files");
            sb.HasKey(s => s.Id);

            sb.Property(x => x.Id).HasColumnName("id");
            sb.Property(x => x.StepType).HasConversion<string>().HasColumnName("step_type");
            sb.Property(x => x.Status).HasConversion<string>().HasColumnName("status");
            sb.Property(x => x.Order).HasColumnName("order");
            sb.Property(x => x.Weight).HasColumnName("weight");
            sb.Property(x => x.ResultData).HasColumnName("result_data").HasColumnType("jsonb");
            sb.Property(x => x.ErrorMessage).HasColumnName("error_message");
            sb.Property(x => x.StartedAt).HasColumnName("started_at");
            sb.Property(x => x.CompletedAt).HasColumnName("completed_at");

            // Создает shadow property (поле есть в EF модели, но его нет в C# классе)
            // Так как таблицы разные, то processing_steps нужна колонка, которая указывает на родителя
            sb.WithOwner().HasForeignKey("VideoProcessingId");
            sb.Property<Guid>("VideoProcessingId").HasColumnName("video_processing_id");
            sb.HasIndex("VideoProcessingId").HasDatabaseName("ix_processing_steps_video_processing_id");

            sb.HasIndex(s => new { s.StepType }).HasDatabaseName("ix_processing_steps_step_type");
            sb.HasIndex(s => new { s.Status }).HasDatabaseName("ix_processing_steps_status");
        });

        builder.HasIndex(x => x.VideoAssetId).HasDatabaseName("ux_video_processing_video_asset_id").IsUnique();
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_video_processing_status");
        builder.HasIndex(x => new { x.Status, x.StartedAt }).HasDatabaseName("ix_video_processing_status_started_at");
    }
}