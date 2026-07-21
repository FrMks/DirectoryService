using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;
using FileService.Domain.Errors;
using FileService.Domain.ValueObjects;
using Shared;

namespace FileService.VideoProcessing.FfmpegProcess;

public static class FfprobeOutputParser
{
    public static Result<VideoMetadata, Error> Parse(string jsonOutput)
    {
        if (string.IsNullOrWhiteSpace(jsonOutput))
            return FileError.InvalidFfprobeOutput("Empty output");

        FfprobeResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<FfprobeResponse>(jsonOutput);
        }
        catch (JsonException ex)
        {
            return FileError.InvalidFfprobeOutput($"JSON parse error: {ex.Message}");
        }

        if (response is null)
            return FileError.InvalidFfprobeOutput("Null response");

        StreamInfo? stream = response.Streams?.FirstOrDefault();
        if (stream is null)
            return FileError.InvalidFfprobeOutput("No video stream found");

        if (stream.Width is null || stream.Height is null)
            return FileError.InvalidFfprobeOutput("Missing resolution");

        if (string.IsNullOrWhiteSpace(stream.CodecName))
            return FileError.InvalidFfprobeOutput("Missing video codec");

        double? durationSeconds = response.Format?.Duration;
        if (durationSeconds is null || durationSeconds <= 0)
            return FileError.InvalidFfprobeOutput("Missing or invalid duration");

        string? container = response.Format?.FormatName;
        if (string.IsNullOrWhiteSpace(container))
            return FileError.InvalidFfprobeOutput("Missing container format");

        var duration = TimeSpan.FromSeconds(durationSeconds.Value);

        return VideoMetadata.Create(duration, stream.Width.Value, stream.Height.Value, stream.CodecName, container);
    }

    private sealed class FfprobeResponse
    {
        [JsonPropertyName("streams")]
        public List<StreamInfo>? Streams { get; set; }

        [JsonPropertyName("format")]
        public FormatInfo? Format { get; set; }
    }

    private sealed class StreamInfo
    {
        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("codec_name")]
        public string? CodecName { get; set; }
    }

    private sealed class FormatInfo
    {
        [JsonPropertyName("duration")]
        [JsonConverter(typeof(StringToDoubleConverter))]
        public double? Duration { get; set; }

        [JsonPropertyName("format_name")]
        public string? FormatName { get; set; }
    }

    private sealed class StringToDoubleConverter : JsonConverter<double?>
    {
        public override double? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? str = reader.GetString();
                if (double.TryParse(str, out double value))
                    return value;
                return null;
            }

            if (reader.TokenType == JsonTokenType.Number)
                return reader.GetDouble();

            return null;
        }

        public override void Write(
            Utf8JsonWriter writer,
            double? value,
            JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteNumberValue(value.Value);
            else
                writer.WriteNullValue();
        }
    }
}