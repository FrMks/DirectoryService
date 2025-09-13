using DirectoryService.Domain.Department;
using DirectoryService.Domain.Department.ValueObject;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Name = DirectoryService.Domain.Department.ValueObject.Name;

namespace DirectoryService.Infrastructure.Postgres.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.HasKey(d => d.Id).HasName("pk_department");
        
        builder.Property(d => d.Id).HasColumnName("department_id");

        builder.Property(d => d.Name)
            .HasConversion(d => d.Value, name => Name.Create(name).Value)
            .HasColumnType("name");
        
        builder.Property(d => d.Identifier)
            .HasConversion(d => d.Value, identifier => Identifier.Create(identifier).Value)
            .HasColumnType("identifier");
    }
}