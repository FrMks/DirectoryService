namespace DirectoryService.Infrastructure.Postgres.DepartmentCleanupBackgroundService;

public class DepartmentCleanupOptions
{
    public bool Enabled { get; set; } = true;
    
    public double IntervalHours { get; set; } = 24;

    public int InactiveDaysThreshold { get; set; } = 30;
}