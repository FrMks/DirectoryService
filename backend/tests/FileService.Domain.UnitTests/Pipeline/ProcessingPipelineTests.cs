using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FileService.Core;
using FileService.Core.Processing;
using FileService.Domain.Entities;
using FileService.Domain.Entities.MediaAssetEntity;
using FileService.Domain.Enums;
using FileService.Domain.MediaProcessing;
using FileService.Domain.ValueObjects;
using FileService.VideoProcessing.Pipeline;
using FileService.VideoProcessing.Pipeline.CleanupService;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Shared;
using Shared.Core.Database;

namespace FileService.Domain.UnitTests.Pipeline;

public sealed class ProcessingPipelineTests
{
    [Fact]
    public async Task ProcessAllStepsAsync_ShouldCompleteVideoAndProcess_WhenAllStepsSucceed()
    {
        VideoAsset video = CreateUploadedVideoAsset();
        var mediaRepository = new InMemoryMediaRepository(video);
        var processingRepository = new InMemoryVideoProcessingRepository();
        var transactionManager = new TestTransactionManager();
        List<StepType> executedSteps = [];
        ProcessingPipeline pipeline = CreatePipeline(
            CreateSuccessfulHandlers(executedSteps),
            mediaRepository,
            processingRepository,
            transactionManager);

        UnitResult<Error> result = await pipeline.ProcessAllStepsAsync(video.Id);

        result.IsSuccess.Should().BeTrue();
        video.Status.Should().Be(MediaStatus.READY);
        video.FinalKey.Should().Be(video.HlsResult.ManifestKey);
        video.Metadata.Should().NotBeNull();

        VideoProcess process = processingRepository.Single();
        process.Status.Should().Be(ProcessingStatus.COMPLETED);
        process.ProgressPercentage.Should().Be(100);
        process.Steps.Should().OnlyContain(step => step.Status == StepStatus.COMPLETED);
        transactionManager.SaveChangesCalls.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ProcessAllStepsAsync_ShouldStopOnFirstStepFailureAndMarkVideoFailed()
    {
        VideoAsset video = CreateUploadedVideoAsset();
        var processingRepository = new InMemoryVideoProcessingRepository();
        List<StepType> executedSteps = [];
        ProcessingPipeline pipeline = CreatePipeline(
            CreateHandlersWithFailure(StepType.GENERATE_HLS, executedSteps),
            new InMemoryMediaRepository(video),
            processingRepository,
            new TestTransactionManager());

        UnitResult<Error> result = await pipeline.ProcessAllStepsAsync(video.Id);

        result.IsFailure.Should().BeTrue();
        video.Status.Should().Be(MediaStatus.FAILED);

        VideoProcess process = processingRepository.Single();
        process.Status.Should().Be(ProcessingStatus.FAILED);
        process.ErrorMessage.Should().Be("Step GENERATE_HLS failed");
        process.Steps.Single(step => step.StepType == StepType.GENERATE_HLS).Status.Should().Be(StepStatus.FAILED);
        executedSteps.Should().Equal(
            StepType.INITIALIZE,
            StepType.DOWNLOAD_SOURCE,
            StepType.EXTRACT_METADATA,
            StepType.GENERATE_HLS);
    }

    [Fact]
    public async Task ProcessAllStepsAsync_ShouldStopOnCancellationAndMarkVideoFailed()
    {
        VideoAsset video = CreateUploadedVideoAsset();
        var processingRepository = new InMemoryVideoProcessingRepository();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        ProcessingPipeline pipeline = CreatePipeline(
            CreateSuccessfulHandlers([]),
            new InMemoryMediaRepository(video),
            processingRepository,
            new TestTransactionManager());

        UnitResult<Error> result = await pipeline.ProcessAllStepsAsync(video.Id, cancellationTokenSource.Token);

        result.IsFailure.Should().BeTrue();
        video.Status.Should().Be(MediaStatus.FAILED);
        processingRepository.Single().Status.Should().Be(ProcessingStatus.FAILED);
    }

    [Fact]
    public async Task ProcessAllStepsAsync_ShouldFail_WhenVideoHasUnsupportedStatusForStart()
    {
        VideoAsset video = CreateUploadedVideoAsset();
        video.MarkPendingProcessing(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        ProcessingPipeline pipeline = CreatePipeline(
            CreateSuccessfulHandlers([]),
            new InMemoryMediaRepository(video),
            new InMemoryVideoProcessingRepository(),
            new TestTransactionManager());

        UnitResult<Error> result = await pipeline.ProcessAllStepsAsync(video.Id);

        result.IsFailure.Should().BeTrue();
        video.Status.Should().Be(MediaStatus.PENDING_PROCESSING);
    }

    [Fact]
    public async Task ProcessAllStepsAsync_ShouldExecuteStepsInDomainOrder()
    {
        VideoAsset video = CreateUploadedVideoAsset();
        List<StepType> executedSteps = [];
        ProcessingPipeline pipeline = CreatePipeline(
            CreateSuccessfulHandlers(executedSteps),
            new InMemoryMediaRepository(video),
            new InMemoryVideoProcessingRepository(),
            new TestTransactionManager());

        UnitResult<Error> result = await pipeline.ProcessAllStepsAsync(video.Id);

        result.IsSuccess.Should().BeTrue();
        executedSteps.Should().Equal(
            StepType.INITIALIZE,
            StepType.DOWNLOAD_SOURCE,
            StepType.EXTRACT_METADATA,
            StepType.GENERATE_HLS,
            StepType.UPLOAD_HLS,
            StepType.GENERATE_PREVIEW,
            StepType.CLEANUP);
    }

    [Fact]
    public async Task ProcessAllStepsAsync_ShouldApplyExpectedStateTransitions()
    {
        VideoAsset video = CreateUploadedVideoAsset();
        var processingRepository = new InMemoryVideoProcessingRepository();
        ProcessingPipeline pipeline = CreatePipeline(
            CreateSuccessfulHandlers([]),
            new InMemoryMediaRepository(video),
            processingRepository,
            new TestTransactionManager());

        UnitResult<Error> result = await pipeline.ProcessAllStepsAsync(video.Id);

        result.IsSuccess.Should().BeTrue();
        video.Status.Should().Be(MediaStatus.READY);
        processingRepository.Single().Status.Should().Be(ProcessingStatus.COMPLETED);
        processingRepository.Single().CompletedAt.Should().NotBeNull();
    }

    private static ProcessingPipeline CreatePipeline(
        IEnumerable<IProcessingStepHandler> handlers,
        IMediaRepository mediaRepository,
        IVideoProcessingRepository processingRepository,
        ITransactionManager transactionManager)
    {
        return new ProcessingPipeline(
            handlers,
            NullLogger<ProcessingPipeline>.Instance,
            mediaRepository,
            processingRepository,
            new TestProcessingCleanupService(),
            transactionManager,
            new ProcessingErrorClassifier());
    }

    private static IReadOnlyList<IProcessingStepHandler> CreateSuccessfulHandlers(List<StepType> executedSteps)
    {
        return
        [
            new RecordingStepHandler(StepType.INITIALIZE, executedSteps),
            new RecordingStepHandler(StepType.DOWNLOAD_SOURCE, executedSteps),
            new RecordingStepHandler(StepType.EXTRACT_METADATA, executedSteps, context =>
            {
                VideoMetadata metadata = VideoMetadata.Create(TimeSpan.FromSeconds(120), 1920, 1080, "h264", "mp4").Value;
                return context.VideoAsset.SetMetadata(metadata).IsSuccess
                    ? UnitResult.Success<Error>()
                    : Error.Failure("metadata.failed", "Failed to set metadata");
            }),
            new RecordingStepHandler(StepType.GENERATE_HLS, executedSteps),
            new RecordingStepHandler(StepType.UPLOAD_HLS, executedSteps),
            new RecordingStepHandler(StepType.GENERATE_PREVIEW, executedSteps),
            new RecordingStepHandler(StepType.CLEANUP, executedSteps),
        ];
    }

    private static IReadOnlyList<IProcessingStepHandler> CreateHandlersWithFailure(
        StepType failingStep,
        List<StepType> executedSteps)
    {
        return Enum.GetValues<StepType>()
            .Select(step => new RecordingStepHandler(step, executedSteps, _ =>
            {
                return step == failingStep
                    ? Error.Failure("step.failed", $"Step {step} failed")
                    : UnitResult.Success<Error>();
            }))
            .ToArray();
    }

    private static VideoAsset CreateUploadedVideoAsset()
    {
        FileName fileName = FileName.Create("lecture.mp4").Value;
        ContentType contentType = ContentType.Create("video/mp4").Value;
        MediaData mediaData = MediaData.Create(fileName, contentType, 1024, 1).Value;
        MediaOwner owner = MediaOwner.ForLesson(Guid.NewGuid()).Value;
        VideoAsset video = VideoAsset.CreateForUpload(Guid.NewGuid(), mediaData, owner).Value;
        video.MarkUploaded(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        return video;
    }

    private sealed class RecordingStepHandler : IProcessingStepHandler
    {
        private readonly List<StepType> _executedSteps;
        private readonly Func<ProcessingContext, UnitResult<Error>> _execute;

        public RecordingStepHandler(
            StepType stepType,
            List<StepType> executedSteps,
            Func<ProcessingContext, UnitResult<Error>>? execute = null)
        {
            StepType = stepType;
            _executedSteps = executedSteps;
            _execute = execute ?? (_ => UnitResult.Success<Error>());
        }

        public StepType StepType { get; }

        public Task<Result<ProcessingContext, Error>> ExecuteAsync(
            ProcessingContext context,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromResult(Result.Failure<ProcessingContext, Error>(
                    Error.Failure("processing.cancelled", "Video processing was cancelled")));
            }

            _executedSteps.Add(StepType);

            UnitResult<Error> result = _execute(context);
            return Task.FromResult(result.IsFailure
                ? Result.Failure<ProcessingContext, Error>(result.Error)
                : Result.Success<ProcessingContext, Error>(context));
        }
    }

    private sealed class TestProcessingCleanupService : IProcessingCleanupService
    {
        public Task<UnitResult<Error>> CleanupUploadedSourceAsync(
            ProcessingContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UnitResult.Success<Error>());
        }

        public UnitResult<Error> CleanupWorkingDirectory(ProcessingContext context)
        {
            return UnitResult.Success<Error>();
        }
    }

    private sealed class InMemoryMediaRepository : IMediaRepository
    {
        private readonly List<MediaAsset> _assets;

        public InMemoryMediaRepository(params MediaAsset[] assets)
        {
            _assets = assets.ToList();
        }

        public Task<Result<Guid, Error>> AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken)
        {
            _assets.Add(mediaAsset);
            return Task.FromResult(Result.Success<Guid, Error>(mediaAsset.Id));
        }

        public void Add(MediaAsset mediaAsset) => _assets.Add(mediaAsset);

        public Task<MediaAsset?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_assets.FirstOrDefault(asset => asset.Id == id));
        }

        public Task<Result<MediaAsset, Error>> GetBy(
            Expression<Func<MediaAsset, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            MediaAsset? asset = _assets.AsQueryable().FirstOrDefault(predicate);
            return Task.FromResult(asset is null
                ? Result.Failure<MediaAsset, Error>(Error.NotFound("media.not.found", "Media asset not found"))
                : Result.Success<MediaAsset, Error>(asset));
        }

        public Task<Result<VideoAsset, Error>> GetVideoAssetBy(
            Expression<Func<VideoAsset, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            VideoAsset? asset = _assets.OfType<VideoAsset>().AsQueryable().FirstOrDefault(predicate);
            return Task.FromResult(asset is null
                ? Result.Failure<VideoAsset, Error>(Error.NotFound("video.not.found", "Video asset not found"))
                : Result.Success<VideoAsset, Error>(asset));
        }

        public Task<Result<IReadOnlyList<MediaAsset>, Error>> GetManyBy(
            Expression<Func<MediaAsset, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<MediaAsset> assets = _assets.AsQueryable().Where(predicate).ToArray();
            return Task.FromResult(Result.Success<IReadOnlyList<MediaAsset>, Error>(assets));
        }

        public Task<int> SaveAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

        public Task UpdateAsync(MediaAsset mediaAsset, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class InMemoryVideoProcessingRepository : IVideoProcessingRepository
    {
        private readonly List<VideoProcess> _processes = [];

        public Task<Result<VideoProcess, Error>> GetBy(
            Expression<Func<VideoProcess, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            VideoProcess? process = _processes.AsQueryable().FirstOrDefault(predicate);
            return Task.FromResult(process is null
                ? Result.Failure<VideoProcess, Error>(Error.NotFound("processing.not.found", "Video processing not found"))
                : Result.Success<VideoProcess, Error>(process));
        }

        public void Add(VideoProcess videoProcess) => _processes.Add(videoProcess);

        public VideoProcess Single() => _processes.Single();
    }

    private sealed class TestTransactionManager : ITransactionManager
    {
        public int SaveChangesCalls { get; private set; }

        public Task<Result<ITransactionScope, Error>> BeginTransaction(
            CancellationToken cancellationToken = default,
            System.Data.IsolationLevel? level = null)
        {
            return Task.FromResult(Result.Failure<ITransactionScope, Error>(
                Error.Failure("transaction.not.supported", "Transactions are not used by these tests")));
        }

        public Task<UnitResult<Error>> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.FromResult(UnitResult.Success<Error>());
        }
    }
}
