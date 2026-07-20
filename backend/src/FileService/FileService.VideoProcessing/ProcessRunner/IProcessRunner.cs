using System.ComponentModel.DataAnnotations;
using System.Xml;
using CSharpFunctionalExtensions;
using Shared;

namespace FileService.VideoProcessing.ProcessRunner;

public interface IProcessRunner
{
    Task<Result<ProcessResult, Error>> RunAsync(
        ProcessCommand command,
        Action<string>? onOutput = null,
        CancellationToken cancellationToken = default);
}
