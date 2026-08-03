using FileService.Domain.MediaProcessing;
using FluentAssertions;

namespace FileService.Domain.UnitTests.MediaProcessing;

public sealed class VideoProcessTests
{
    [Fact]
    public void Constructor_ShouldInitializeProcessingPipeline()
    {
        Guid videoAssetId = Guid.NewGuid();
        DateTime beforeCreate = DateTime.UtcNow;

        var process = new VideoProcess(videoAssetId);

        process.Id.Should().NotBeEmpty();
        process.VideoAssetId.Should().Be(videoAssetId);
        process.Status.Should().Be(ProcessingStatus.IN_PROGRESS);
        process.ProgressPercentage.Should().Be(0);
        process.StartedAt.Should().BeOnOrAfter(beforeCreate);
        process.CompletedAt.Should().BeNull();
        process.CurrentStep.Should().BeNull();

        process.Steps.Should().HaveCount(7);
        process.Steps.Should().OnlyContain(step => step.Status == StepStatus.PENDING);
        process.Steps.Select(step => step.Order).Should().Equal(1, 2, 3, 4, 5, 6, 7);
        process.Steps.Select(step => step.StepType).Should().Equal(
            StepType.INITIALIZE,
            StepType.DOWNLOAD_SOURCE,
            StepType.EXTRACT_METADATA,
            StepType.GENERATE_HLS,
            StepType.UPLOAD_HLS,
            StepType.GENERATE_PREVIEW,
            StepType.CLEANUP);
        process.Steps.Select(step => step.Weight).Should().Equal(0, 0, 10, 60, 15, 10, 5);
    }

    [Fact]
    public void ProcessNextStep_ShouldStartFirstPendingStep()
    {
        var process = new VideoProcess(Guid.NewGuid());

        var result = process.ProcessNextStep();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StepType.Should().Be(StepType.INITIALIZE);
        result.Value.Status.Should().Be(StepStatus.IN_PROGRESS);
        result.Value.StartedAt.Should().NotBeNull();
        process.CurrentStep.Should().Be(result.Value);
    }

    [Fact]
    public void ProcessNextStep_ShouldReturnCurrentStep_WhenStepAlreadyInProgress()
    {
        var process = new VideoProcess(Guid.NewGuid());
        var firstResult = process.ProcessNextStep();

        var secondResult = process.ProcessNextStep();

        secondResult.IsSuccess.Should().BeTrue();
        secondResult.Value.Should().BeSameAs(firstResult.Value);
        process.Steps.Count(step => step.Status == StepStatus.IN_PROGRESS).Should().Be(1);
    }

    [Fact]
    public void CompleteCurrentStep_ShouldCompleteActiveStepAndRecalculateProgress()
    {
        var process = new VideoProcess(Guid.NewGuid());
        process.ProcessNextStep();

        var completeInitializeResult = process.CompleteCurrentStep("initialized");

        completeInitializeResult.IsSuccess.Should().BeTrue();
        process.Steps[0].Status.Should().Be(StepStatus.COMPLETED);
        process.Steps[0].ResultData.Should().Be("initialized");
        process.Steps[0].CompletedAt.Should().NotBeNull();
        process.ProgressPercentage.Should().Be(0);

        process.ProcessNextStep();
        var completeDownloadResult = process.CompleteCurrentStep("downloaded");

        completeDownloadResult.IsSuccess.Should().BeTrue();
        process.Steps[1].Status.Should().Be(StepStatus.COMPLETED);
        process.ProgressPercentage.Should().Be(0);

        process.ProcessNextStep();
        var completeMetadataResult = process.CompleteCurrentStep("metadata");

        completeMetadataResult.IsSuccess.Should().BeTrue();
        process.Steps[2].Status.Should().Be(StepStatus.COMPLETED);
        process.ProgressPercentage.Should().Be(10);
    }

    [Fact]
    public void ProcessNextStep_ShouldCompleteProcess_WhenAllStepsAreCompleted()
    {
        var process = new VideoProcess(Guid.NewGuid());

        foreach (ProcessingStep step in process.Steps)
        {
            process.ProcessNextStep().IsSuccess.Should().BeTrue();
            process.CompleteCurrentStep().IsSuccess.Should().BeTrue();
        }

        var result = process.ProcessNextStep();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        process.Status.Should().Be(ProcessingStatus.COMPLETED);
        process.ProgressPercentage.Should().Be(100);
        process.CompletedAt.Should().NotBeNull();
        process.Steps.Should().OnlyContain(step => step.Status == StepStatus.COMPLETED);
    }

    [Fact]
    public void CompleteCurrentStep_ShouldFail_WhenNoStepIsActive()
    {
        var process = new VideoProcess(Guid.NewGuid());

        var result = process.CompleteCurrentStep();

        result.IsFailure.Should().BeTrue();
        process.ProgressPercentage.Should().Be(0);
        process.Steps.Should().OnlyContain(step => step.Status == StepStatus.PENDING);
    }

    [Fact]
    public void FailCurrentStep_ShouldFailActiveStepWithoutFailingProcess()
    {
        var process = new VideoProcess(Guid.NewGuid());
        process.ProcessNextStep();

        var result = process.FailCurrentStep("ffmpeg failed");

        result.IsSuccess.Should().BeTrue();
        process.Status.Should().Be(ProcessingStatus.IN_PROGRESS);
        process.CurrentStep.Should().BeNull();
        process.Steps[0].Status.Should().Be(StepStatus.FAILED);
        process.Steps[0].ErrorMessage.Should().Be("ffmpeg failed");
    }

    [Fact]
    public void Fail_ShouldMoveProcessToFailedState()
    {
        var process = new VideoProcess(Guid.NewGuid());

        var result = process.Fail("video processing failed", isCritical: true);

        result.IsSuccess.Should().BeTrue();
        process.Status.Should().Be(ProcessingStatus.FAILED);
        process.ErrorMessage.Should().Be("video processing failed");
        process.IsCriticalError.Should().BeTrue();
        process.CompletedAt.Should().NotBeNull();
        process.CanRetry().Should().BeFalse();
    }

    [Fact]
    public void Reset_ShouldRestoreFailedProcessToInitialProcessingState()
    {
        var process = new VideoProcess(Guid.NewGuid());
        process.ProcessNextStep();
        process.FailCurrentStep("temporary error");
        process.Fail("process failed").IsSuccess.Should().BeTrue();

        var result = process.Reset();

        result.IsSuccess.Should().BeTrue();
        process.Status.Should().Be(ProcessingStatus.IN_PROGRESS);
        process.ProgressPercentage.Should().Be(0);
        process.ErrorMessage.Should().BeNull();
        process.CompletedAt.Should().BeNull();
        process.IsCriticalError.Should().BeFalse();
        process.CurrentStep.Should().BeNull();
        process.Steps.Should().OnlyContain(step => step.Status == StepStatus.PENDING);
        process.Steps.Should().OnlyContain(step => step.ResultData == null && step.ErrorMessage == null);
        process.Steps.Should().OnlyContain(step => step.StartedAt == null && step.CompletedAt == null);
    }

    [Fact]
    public void ScheduleRetry_ShouldIncrementRetryCountAndSetNextRetryAt()
    {
        var process = new VideoProcess(Guid.NewGuid());
        process.Fail("temporary error").IsSuccess.Should().BeTrue();
        DateTime nextRetryAt = DateTime.UtcNow.AddMinutes(5);

        var result = process.ScheduleRetry(nextRetryAt);

        result.IsSuccess.Should().BeTrue();
        process.RetryCount.Should().Be(1);
        process.NextRetryAt.Should().Be(nextRetryAt);
        process.CanRetry().Should().BeTrue();
    }

    [Fact]
    public void ScheduleRetry_ShouldFail_WhenMaxRetriesExceeded()
    {
        var process = new VideoProcess(Guid.NewGuid());
        process.Fail("temporary error").IsSuccess.Should().BeTrue();

        process.ScheduleRetry(DateTime.UtcNow.AddMinutes(1)).IsSuccess.Should().BeTrue();
        process.ScheduleRetry(DateTime.UtcNow.AddMinutes(2)).IsSuccess.Should().BeTrue();
        process.ScheduleRetry(DateTime.UtcNow.AddMinutes(3)).IsSuccess.Should().BeTrue();

        var result = process.ScheduleRetry(DateTime.UtcNow.AddMinutes(4));

        result.IsFailure.Should().BeTrue();
        process.RetryCount.Should().Be(3);
        process.CanRetry().Should().BeFalse();
    }
}
