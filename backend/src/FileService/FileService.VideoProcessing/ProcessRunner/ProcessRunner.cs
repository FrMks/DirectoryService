using System.Diagnostics;
using CSharpFunctionalExtensions;
using Shared;

namespace FileService.VideoProcessing.ProcessRunner;

public class ProcessRunner : IProcessRunner
{
    public Task<Result<ProcessResult, Error>> RunAsync(
        ProcessCommand command,
        Action<string>? onOutput = null,
        CancellationToken cancellationToken = default)
    {
        using var process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command.ExecutableFile,
                Arguments = command.Arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
    }
}