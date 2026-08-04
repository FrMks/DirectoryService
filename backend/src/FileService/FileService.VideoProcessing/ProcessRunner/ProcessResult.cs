namespace FileService.VideoProcessing.ProcessRunner;

public record ProcessResult(int ExitCode, string StandardOutput, string StandardError);