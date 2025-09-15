using DirectoryService.Domain;
using DirectoryService.Domain.Department;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Locations.ValueObjects;
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