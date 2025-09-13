using DirectoryService.Domain;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Locations.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DirectoryService.Infrastructure.Postgres.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location> 
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");
        
        builder.HasKey(l => l.Id).HasName("pk_locations");

        builder.Property(l => l.Id)
            .HasColumnName("id");

        // TODO: Вопрос: мне кажется, что это потенциально опасная операция, потому что,
        // если у меня не валидные данные, то мне выкенет Exception
        builder.Property(l => l.Name)
            .HasConversion(l => l.Value, name => Name.Create(name).Value)
            .HasColumnName("name");

        // builder.OwnsOne(l => l.Address, addressBuilder =>
        builder.ComplexProperty(l => l.Address, addressBuilder =>
        {
            addressBuilder
                .Property(l => l.Street)
                .IsRequired()
                .HasMaxLength(LengthConstants.LENGTH100)
                .HasColumnName("street");
            
            addressBuilder
                .Property(l => l.City)
                .IsRequired()
                .HasMaxLength(LengthConstants.LENGTH60)
                .HasColumnName("city");
            
            addressBuilder
                .Property(l => l.Country)
                .IsRequired()
                .HasMaxLength(LengthConstants.LENGTH60)
                .HasColumnName("country");
        });
        // Если нужно, чтобы был nullable VO, то:
        // builder.Navigation(l => l.Address).IsRequired(false);

        builder.Property(l => l.Timezone)
            .HasConversion(l => l.Value, timezone => Timezone.Create(timezone).Value)
            .HasColumnName("timezone");
        
        builder.Property(l => l.IsActive)
            .HasColumnName("is_active");
        
        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at");
        
        builder.Property(l => l.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasMany(l => l.DepartmentLocations)
            .WithOne()
            .HasForeignKey(l => l.LocationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}