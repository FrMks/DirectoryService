namespace FileService.Infrastructure.Postgres.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    // Сколько количество записей будет браться постепенно для обработки.
    public int BatchSize { get; set; } = 100;

    public int MaxRetries { get; set; } = 5;

    public int InitialRetryDelaySeconds { get; set; } = 1;
}