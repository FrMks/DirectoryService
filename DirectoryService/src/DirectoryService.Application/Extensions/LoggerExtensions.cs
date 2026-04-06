using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Extensions;

public static class LoggerExtensions
{
    public static void LogErrors(this ILogger logger, Errors errors, string messagePrefix)
    {
        foreach (var error in errors)
        {
            logger.LogError("{messagePrefix}: {error}", messagePrefix, error.Message);
        }
    }
}
