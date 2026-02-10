using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.Locations.ValueObjects;
using DirectoryService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DirectoryService.Infrastructure.Postgres.Configurations;

public class DepartmentLocationConfiguration : IEntityTypeConfiguration<DepartmentLocation>
{
    public void Configure(EntityTypeBuilder<DepartmentLocation> builder)
    {
        builder.ToTable("department_locations");

        builder.HasKey(dl => dl.Id).HasName("pk_department_locations");

        builder.Property(dl => dl.Id)
            .HasConversion(dl => dl.Value, departmentLocationId => DepartmentLocationId.FromValue(departmentLocationId))
            .HasColumnName("id");

        builder.Property(dl => dl.LocationId)
            .HasConversion(dl => dl.Value, locationId => LocationId.FromValue(locationId))
            .HasColumnName("location_id")
            .IsRequired();

        builder.Property(dl => dl.DepartmentId)
            .HasConversion(dl => dl.Value, departmentId => DepartmentId.FromValue(departmentId))
            .HasColumnName("department_id")
            .IsRequired();
    }
}