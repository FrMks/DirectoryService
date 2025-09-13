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
        
        builder.Property(d => d.Id).HasColumnName("id");

        builder.Property(d => d.Name)
            .HasConversion(d => d.Value, name => Name.Create(name).Value)
            .HasColumnName("name");
        
        builder.Property(d => d.Identifier)
            .HasConversion(d => d.Value, identifier => Identifier.Create(identifier).Value)
            .HasColumnName("identifier");

        builder.Property(d => d.ParentId)
            .HasColumnName("parent_id")
            .IsRequired(false);

        builder.Property(d => d.Path)
            .HasConversion(d => d.Value, path => Path.Create(path).Value)
            .HasColumnName("path");
    }
}