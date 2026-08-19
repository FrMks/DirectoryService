using FileService.Domain;
using FileService.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Postgres.Configurations;

public class ProcessingJobOutboxMessageConfiguration : IEntityTypeConfiguration<ProcessingJobOutboxMessage>
{
    public void Configure(EntityTypeBuilder<ProcessingJobOutboxMessage> builder)
    {
        builder.ToTable("processing_job_outbox_messages");

        builder.HasKey(message => message.Id);

        builder
            .Property(message => message.MediaAssetId)
            .HasColumnName("media_asset_id")
            .IsRequired();

        builder
            .Property(message => message.Status)
            .HasConversion<string>()
            .HasMaxLength(LenghtConstants.LENGTH32)
            .HasColumnName("status")
            .IsRequired();

        builder
            .Property(message => message.Attempts)
            .HasColumnName("attempts")
           .IsRequired();

        builder
            .Property(message => message.MaxRetries)
            .HasColumnName("max_retries")
            .IsRequired();

        builder
            .Property(message => message.NextAttemptAt)
            .HasColumnName("next_attempt_at")
            .IsRequired();

        builder
            .Property(message => message.StartedProcessingAt)
            .HasColumnName("started_processing_at");

        builder
            .Property(message => message.LastAttemptedAt)
            .HasColumnName("last_attempted_at");

        builder
            .Property(message => message.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(2000);

        builder
            .Property(message => message.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder
            .Property(message => message.CompletedAt)
            .HasColumnName("completed_at");

        builder.HasIndex(message => new
        {
            message.Status,
            message.NextAttemptAt
        })
        .HasDatabaseName(
            "IX_processing_job_outbox_messages_status_next_attempt_at");

        builder.HasIndex(message => message.MediaAssetId)
            .IsUnique()
            .HasDatabaseName(
                "UX_processing_job_outbox_messages_media_asset_id");
    }
}
