using DirectoryService.Domain.Positions;
using DirectoryService.Domain.Positions.ValueObject;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DirectoryService.Infrastructure.Postgres.Configurations;

public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("positions");
        
        builder.HasKey(p => p.Id).HasName("pk_positions");

        builder.Property(p => p.Id)
            .HasConversion(p => p.Value, positionId => PositionId.FromValue(positionId))
            .HasColumnName("id");

        builder.Property(p => p.Name)
            .HasConversion(p => p.Value, name => Name.Create(name).Value)
            .HasColumnName("name");

        builder.Property(p => p.Description)
            .HasConversion(p => p.Value, description => Description.Create(description).Value)
            .HasColumnName("description");

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active");
        
        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at");
        
        builder.Property(p => p.UpdateAt)
            .HasColumnName("update_at");

        builder.HasMany(p => p.DepartmentPositions)
            .WithOne()
            .HasForeignKey(dp => dp.PositionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}