using DirectoryService.Domain;
using DirectoryService.Domain.Department;
using DirectoryService.Domain.Department.ValueObject;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Name = DirectoryService.Domain.Department.ValueObject.Name;
using Path = DirectoryService.Domain.Department.ValueObject.Path;

namespace DirectoryService.Infrastructure.Postgres.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");
        
        builder.HasKey(d => d.Id).HasName("pk_department");
        
        builder.Property(d => d.Id)
            .HasConversion(d => d.Value, departmentId => DepartmentId.FromValue(departmentId))
            .HasColumnName("id");

        builder.Property(d => d.Name)
            .HasConversion(d => d.Value, name => Name.Create(name).Value)
            .HasColumnName("name")
            .HasMaxLength(LengthConstants.LENGTH150);
        
        builder.Property(d => d.Identifier)
            .HasConversion(d => d.Value, identifier => Identifier.Create(identifier).Value)
            .HasColumnName("identifier");

        builder.Property(d => d.ParentId)
            .HasColumnName("parent_id")
            .IsRequired(false);

        builder.Property(d => d.Path)
            .HasConversion(d => d.Value, path => Path.Create(path).Value)
            .HasColumnName("path")
            .HasColumnType("ltree");
        
        builder.HasIndex(x => x.Path).HasMethod("gist").HasDatabaseName("idx_departments_path");
        
        builder.Property(d => d.Depth)
            .HasConversion(d => d.Value, depth => Depth.Create(depth).Value)
            .HasColumnName("depth");

        builder.Property(d => d.IsActive)
            .HasColumnName("is_active");

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at");
        
        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at");
        
        builder.HasMany(d => d.DepartmentLocations)
            .WithOne()
            .HasForeignKey(d => d.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(d => d.DepartmentPositions)
            .WithOne()
            .HasForeignKey(dp => dp.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}