namespace FileService.Infrastructure.Postgres.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    // Сколько количество записей будет браться постепенно для обработки.
    public int BatchSize { get; private set; } = 100;

    public int MaxRetries { get; private set; } = 5;

    public int InitializeRetryDelaySeconds { get; private set; } = 1;
}