namespace DirectoryService.Contracts.Departments.GetTopDepartments;

public class DepartmentDapperDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string Path { get; set; } = string.Empty;
    public short Depth { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int PositionsCount { get; set; }
}
