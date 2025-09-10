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
        
        builder.HasKey(l => l.Id).HasName("id");

        // TODO: в зависимости от ответа переделать или оставить
        builder.Property(l => l.Name)
            .HasConversion(l => l.Value, name => new Name(name));

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
    }
}