using System.Security.Cryptography.X509Certificates;

namespace DirectoryService.Contracts.Departments.GetTopDepartments;

public record DepartmentDto
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string Path { get; set; } = string.Empty;
    public short Depth { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}