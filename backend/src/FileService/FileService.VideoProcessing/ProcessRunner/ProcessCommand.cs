using System.Text.RegularExpressions;

namespace FileService.VideoProcessing.ProcessRunner;

public partial record ProcessCommand(string ExecutableFile, string Arguments)
{
    public string NormalizedArguments => NormalizeWhitespace(Arguments);

    private static string NormalizeWhitespace(string input)
        => WhitespaceRegex().Replace(input.Trim(), " ");

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();
}
