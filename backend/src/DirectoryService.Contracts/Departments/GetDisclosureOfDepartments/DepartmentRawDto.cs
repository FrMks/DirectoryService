namespace DirectoryService.Contracts.Departments.GetDisclosureOfDepartments;

public class DepartmentRawDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Identifier { get; set; } = null!;

    public string Path { get; set; } = null!;

    public short Depth { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid? ParentId { get; set; }

    public bool HasMoreChildren { get; set; }
}