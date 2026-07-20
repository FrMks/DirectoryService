using System.Diagnostics;
using System.Text;
using CSharpFunctionalExtensions;
using Shared;

namespace FileService.VideoProcessing.ProcessRunner;

// Я умею запускать команду
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
                FileName = command.ExecutableFile, // Путь что мы запускаем (либо ffmpeg либо ffprobe)
                Arguments = command.Arguments, // Строка аргументов командной строки. Например для ffprobe -v error -print_format json -show_streams "input.mp4"
                RedirectStandardOutput = true, // Не выводит stdout в консоль, а дай .NET-приложению его прочитать. Для ffprobe это особенно важно, потому что он может вернуть JSON metadata именно в stdout. То есть без redirect ты бы не получил этот JSON в коде
                RedirectStandardError = true, // stderr тоже читать из .NET. У ffmpeg это критично: ffmpeg очень много диагностической информации, прогресса и ошибок пишет именно в stderr, даже когда команда в целом работает. 
                UseShellExecute = false, // Запускаем процесс напрямую, без shell (cmd.exe, PowerShell, проводник Windows). Это нужно, чтобы работали RedirectStandardOutput = true и RedirectStandardError = true
                CreateNoWindow = true, // Не открывать отдельное консольное окно для ffmpeg.exe
            },
        };

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


    }
}