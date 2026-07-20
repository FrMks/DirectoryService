using System.Diagnostics;
using System.Text;
using CSharpFunctionalExtensions;
using FileService.Domain.Errors;
using Microsoft.Extensions.Logging;
using Pipelines.Sockets.Unofficial.Arenas;
using Shared;

namespace FileService.VideoProcessing.ProcessRunner;

// Я умею запускать команду
public class ProcessRunner : IProcessRunner
{
    private readonly ILogger<ProcessRunner> _logger;

    public ProcessRunner(ILogger<ProcessRunner> logger)
    {
        _logger = logger;
    }

    public async Task<Result<ProcessResult, Error>> RunAsync(
        ProcessCommand command,
        Action<string>? onOutput = null,
        CancellationToken cancellationToken = default)
    {
        using var process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command.ExecutableFile, // Путь что мы запускаем (либо ffmpeg либо ffprobe)
                Arguments = command.Arguments, // Строка аргументов командной строки. Например для ffprobe -v error -print_format json -show_streams "input.mp4"
                RedirectStandardOutput = true, // Не выводит stdout в консоль, а дай .NET-приложению его прочитать. Для ffprobe это особенно важно, потому что он может вернуть JSON metadata именно в stdout. То есть без redirect ты бы не получил этот JSON в коде
                RedirectStandardError = true, // stderr тоже читать из .NET. У ffmpeg это критично: ffmpeg очень много диагностической информации, прогресса и ошибок пишет именно в stderr, даже когда команда в целом работает. 
                UseShellExecute = false, // Запускаем процесс напрямую, без shell (cmd.exe, PowerShell, проводник Windows). Это нужно, чтобы работали RedirectStandardOutput = true и RedirectStandardError = true
                CreateNoWindow = true, // Не открывать отдельное консольное окно для ffmpeg.exe
            },
        };

        // Выполняем синхронно
        StringBuilder outputBuilder = new();
        StringBuilder errorBuilder = new();

        // У ffprobe то после команды в stdout придет JSON
        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is null)
                return;

            outputBuilder.AppendLine(args.Data);
            onOutput?.Invoke(args.Data);
        };

        // Сюда попадает то, что процесс пишет в диагностический поток. stderr не всегда значит "ошибка".
        // Это просто отдельный канал для логов, предпреждений, прогресса и ошибок
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is null)
                return;

            errorBuilder.Append(args.Data);
            onOutput?.Invoke(args.Data);
        };

        _logger.LogInformation("Starting process: {FileName} {Arguments}", command.ExecutableFile, command.Arguments);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Process was cancelled: {FileName} {Arguments}", command.ExecutableFile, command.Arguments);
            return Error.Failure("operation.cancelled", "Operation was cancelled in ProcessRunner");
        }

        ProcessResult result = new(process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());

        if (result.ExitCode != 0)
        {
            _logger.LogError(
                "Process failed: {FileName} {Arguments} ExitCode: {ExitCode} Error: {Error}",
                command.ExecutableFile, command.Arguments, result.ExitCode, result.StandardError);
            return FileError.ProcessFailed();
        }

        return result;
    }
}