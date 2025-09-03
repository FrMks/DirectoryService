namespace DirectoryService.Domain;

public class DepartmentLocation
{
    public DepartmentLocation() { }
    
    public DepartmentLocation() { }
    
    public Guid Id { get; private set; }
    
    public Guid LocationId { get; private set; }
    
    public Guid DepartmentId { get; private set; }
    
    public Location Location { get; private set; }
    
    public Depatment Depatment { get; private set; }
}

public class Depatment
{
}

public class Location
{
}