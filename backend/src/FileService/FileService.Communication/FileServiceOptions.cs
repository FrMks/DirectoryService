using System.Net.Http.Headers;

namespace FileService.Communication;

public record FileServiceOptions
{
    public const string SectionName = "FileService";

    public string Url { get; init; } = string.Empty;

    // Максимальное время всей операции вместе с повторными попытками
    public int TimeoutSeconds { get; init; } = 7;

    // Максимальное время одной попытки 
    public int AttemptTimeoutSeconds { get; init; } = 2;

    public int RetryCount { get; init; } = 2;

    public int RetryDelayMilliseconds { get; init; } = 200;

    public double CircuitBreakerFailureRatio { get; init; } = 0.5;

    public int CircuitBreakerMinimumThroughput { get; init; } = 5;

    public int CircuitBreakerSamplingSeconds { get; init; } = 10;

    public int CircuitBreakerBreakSeconds { get; init; } = 15;
}