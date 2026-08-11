using Shared;

namespace FileService.Core.Processing;

public sealed class ProcessingErrorClassifier : IProcessingErrorClassifier
{
    private static readonly HashSet<string> TransientErrorCodes =
    [
        "operation.cancelled",
        "network.issue",
        "database",
        "database.error",
        "transaction.commit.failed",
    ];

    public ProcessingErrorKind Classify(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return TransientErrorCodes.Contains(error.Code)
            ? ProcessingErrorKind.Transient
            : ProcessingErrorKind.Permanent;
    }
}
