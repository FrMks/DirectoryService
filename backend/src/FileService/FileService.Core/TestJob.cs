using Microsoft.Extensions.Logging;
using Quartz;

namespace FileService.Core;

[DisallowConcurrentExecution]
public class TestJob : IJob
{
    private readonly ILogger<TestJob> _logger;

    public TestJob(ILogger<TestJob> logger)
    {
        _logger = logger;
    }

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation(
            "TestJob executed at {Time}. FireTimeUtc: {FireTime}",
            DateTime.UtcNow,
            context.FireTimeUtc);

        return Task.CompletedTask;
    }
}
