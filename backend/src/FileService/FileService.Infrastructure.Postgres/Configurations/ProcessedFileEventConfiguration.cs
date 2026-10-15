using FileService.Domain;
using FileService.Domain.IntegrationEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Postgres.Configurations;

public sealed class ProcessedFileEventConfiguration : IEntityTypeConfiguration<ProcessedFileEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedFileEvent> builder)
    {
        builder.ToTable("processed_file_events");

        builder.HasKey(processedEvent => processedEvent.EventId);

        builder
            .Property(processedEvent => processedEvent.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder
            .Property(processedEvent => processedEvent.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(LenghtConstants.LENGTH200)
            .IsRequired();

        builder
            .Property(processedEvent => processedEvent.ProcessedAt)
            .HasColumnName("processed_at")
            .IsRequired();
    }
}
