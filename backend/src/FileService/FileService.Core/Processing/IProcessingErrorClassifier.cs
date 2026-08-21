using Shared;

namespace FileService.Core.Processing;

public interface IProcessingErrorClassifier
{
    ProcessingErrorKind Classify(Error error);
}
