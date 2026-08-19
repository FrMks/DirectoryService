using FileService.Domain.Outbox;
using FluentAssertions;

namespace FileService.Domain.UnitTests.Outbox;

public sealed class ProcessingJobOutboxMessageTests
{
    [Fact]
    public void Create_ShouldInitializePendingMessage()
    {
        Guid mediaAssetId = Guid.NewGuid();

        ProcessingJobOutboxMessage message = ProcessingJobOutboxMessage.Create(
            mediaAssetId,
            maxRetries: 5);

        message.Id.Should().NotBeEmpty();
        message.MediaAssetId.Should().Be(mediaAssetId);
        message.Status.Should().Be(ProcessingJobOutboxStatus.Pending);
        message.Attempts.Should().Be(0);
        message.MaxRetries.Should().Be(5);
        message.NextAttemptAt.Should().BeCloseTo(
            DateTimeOffset.UtcNow,
            precision: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkFailed_ShouldUseExponentialBackoff()
    {
        ProcessingJobOutboxMessage message = ProcessingJobOutboxMessage.Create(
            Guid.NewGuid(),
            maxRetries: 5);

        message.IncrementAttempts();
        message.MarkFailed("first failure", initialRetryDelaySeconds: 1);
        DateTimeOffset firstRetryAt = message.NextAttemptAt;

        message.IncrementAttempts();
        message.MarkFailed("second failure", initialRetryDelaySeconds: 1);

        message.Status.Should().Be(ProcessingJobOutboxStatus.Failed);
        message.LastError.Should().Be("second failure");
        message.Attempts.Should().Be(2);
        (message.NextAttemptAt - firstRetryAt).Should().BeCloseTo(
            TimeSpan.FromSeconds(1),
            precision: TimeSpan.FromMilliseconds(250));
    }

    [Fact]
    public void ResetProcessingToPending_ShouldKeepAttemptsAndClearStartedTime()
    {
        ProcessingJobOutboxMessage message = ProcessingJobOutboxMessage.Create(
            Guid.NewGuid(),
            maxRetries: 5);

        message.SwitchStatusTo(ProcessingJobOutboxStatus.Processing);
        message.IncrementAttempts();
        message.SetTimeWhenStartProcessing();

        message.ResetProcessingToPending();

        message.Status.Should().Be(ProcessingJobOutboxStatus.Pending);
        message.Attempts.Should().Be(1);
        message.StartedProcessingAt.Should().BeNull();
        message.NextAttemptAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void MarkCompleted_ShouldSetCompletedStatusAndClearError()
    {
        ProcessingJobOutboxMessage message = ProcessingJobOutboxMessage.Create(
            Guid.NewGuid(),
            maxRetries: 5);

        message.IncrementAttempts();
        message.MarkFailed("temporary failure", initialRetryDelaySeconds: 1);

        message.MarkCompleted();

        message.Status.Should().Be(ProcessingJobOutboxStatus.Completed);
        message.LastError.Should().BeNull();
        message.CompletedAt.Should().NotBeNull();
    }
}
