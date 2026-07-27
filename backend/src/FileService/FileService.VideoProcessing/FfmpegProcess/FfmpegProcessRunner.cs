using System.Diagnostics;
using CSharpFunctionalExtensions;
using FileService.Domain.Entities;
using FileService.Domain.ValueObjects;
using FileService.VideoProcessing.ProcessRunner;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Framework.EndpointResults;

namespace FileService.VideoProcessing.FfmpegProcess;

// Я умею вызывать ffmpeg/ffprobe правильными командами
public class FfmpegProcessRunner : IFfmpegProcessRunner
{
    private readonly VideoProcessingOptions _options;
    private readonly IProcessRunner _processRunner;

    public FfmpegProcessRunner(
        IOptions<VideoProcessingOptions> options,
        IProcessRunner processRunner)
    {
        _options = options.Value;
        _processRunner = processRunner;
    }

    public async Task<Result<VideoMetadata, Error>> ExtractMetadataAsync(
        string inputFileUrl,
        CancellationToken ct = default)
    {
        string arguments = BuildFfprobeArguments(inputFileUrl);
        var command = new ProcessCommand(_options.FfprobePath, arguments);

        Result<ProcessResult, Error> processResult = await _processRunner.RunAsync(
            command,
            cancellationToken: ct);
        if (processResult.IsFailure)
            return processResult.Error;

        return FfprobeOutputParser.Parse(processResult.Value.StandardOutput);
    }

    public async Task<UnitResult<Error>> GenerateHlsAsync(
        string inputFileUrl, // What url will set to output directory
        string outputDirectory, // Where HLS will set
        CancellationToken cancellationToken = default)
    {
        string arguments = BuildFfmpegHlsArguments(inputFileUrl, outputDirectory);

        var command = new ProcessCommand(_options.FfmpegPath, arguments);

        Result<ProcessResult, Error> processResult = await _processRunner.RunAsync(command, cancellationToken: cancellationToken);
        if (processResult.IsFailure)
            return processResult.Error;

        return Result.Success<Error>();
    }

    private static string BuildFfprobeArguments(string inputFileUrl)
    {
        return
            $"""
            -v error
            -select_streams v:0
            -show_entries stream=width,height,codex_name:format=duration,format_name
            -of json
            "{inputFileUrl}"
            """;
    }

    private string BuildFfmpegHlsArguments(string inputFileUrl, string outputDirectory)
    {
        // Если у нас есть видео карта, то мы ее используем
        string hwaccel = _options.UseHardwareAcceleration
            ? "-hwaccel cuda -hwaccel_output_format cuda "
            : string.Empty;

        // Без -y ffmpeg может оставиться и спросить. Что вот файл существует. Мы его перезаписываем?
        // -starts будет писать прогресс. ffmpeg может выводить только количество кадров, скорость, время, битрейт и т.д.
        return $"-y -stats -loglevel error {hwaccel}-i \"{inputFileUrl}\" " + // -logLevel error показывает только ошибки
            "-filter_complex \"" + // начинает список фильтров. Нам это надо, потому что мы разделяем одно видео на несколько вариантов, а потом объединяем
            "[0:v]split=3[v0][v1][v2]; " + // Берем видео поток как первый вход [0:v] и разделяем на 3 видео ветки
            "[v0]scale=w=-2:h=360[v0out]; " +
            "[v1]scale=w=-2:h=720[v1out]; " +
            "[v2]scale=w=-2:h=1080[v2out]; " +
            "[0:a]asplit=3[a0][a1][a2]\" " + // Берем аудио поток и разделяем на три ветки.
            BuildVideoMappings() +
            BuildAudioMappings() +
            "-f hls " + // вывод в hls
            "-var_stream_map \"v:0,a:0,name:360p v:1,a:1,name:720p v:2,a:2,name:1080p\" " + // как группируем видео
            "-hls_time 4 " + // разбивает видео по 4 секунды
            "-hls_list_size 0 " + // все видео по 4 секунды в один плейлист
            "-hls_segment_type mpegts " +
            "-hls_playlist_type vod " +
            $"-hls_segment_filename {outputDirectory}/{VideoAsset.SEGMENT_FILE_PATTERN} " +
            $"-master_pl_name {VideoAsset.MASTER_PLAYLIST_NAME} " +
            $"{outputDirectory}/{VideoAsset.STREAM_PLAYLIST_PATTERN}";
    }

    private string BuildVideoMappings()
    {
        string encoder = _options.VideoEncoder;
        string preset = _options.VideoPreset;

        // -map \"[v0out]\" означает использовать фильтрованный вывод на выходе первого потока 
        // -c:v:number кодек для первого видео 
        // -preset {preset} Скорость/сжатие показывается
        // -maxrate:v:0 2M - максимальный битрейте. 
        // -bufsize:v:0 2M Размер буфера регулирует скорость.
        // -g 20 расстояние между ключевыми кадрами. Для fps 30, -g 20 означает ключевые кадры примеру каждые 0.66 секунд.
        return $"-map \"[v0out]\" -c:v:0 {encoder} -preset {preset} -b:v:0 2M -maxrate:v:0 2M -bufsize:v:0 2M -g 20 " +
               $"-map \"[v1out]\" -c:v:1 {encoder} -preset {preset} -b:v:1 3M -maxrate:v:1 3M -bufsize:v:1 3M -g 20 " +
               $"-map \"[v2out]\" -c:v:2 {encoder} -preset {preset} -b:v:2 5M -maxrate:v:2 5M -bufsize:v:2 2M -g 20 ";
    }

    private string BuildAudioMappings() =>
        "-map \"[a0]\" -c:a:0 -b:a:0 96k -ac 2 " +
        "-map \"[a1]\" -c:a:1 -b:a:1 96k -ac 2 " +
        "-map \"[a2]\" -c:a:2 -b:a:2 96k -ac 2 ";
}
