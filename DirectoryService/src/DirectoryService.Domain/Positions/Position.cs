namespace DirectoryService.Domain.Positions;

public class Position
{
    private Position(Guid id)
    {
        Id = id;
    }
    
    public Guid Id { get; private set; }
    
    public void SetId(Guid id) => Id = id;
}