# FileService video processing branch

- This branch implements FS-9/FS-10 groundwork for video processing without real FFmpeg. FS-11 should replace mock steps with ffprobe/transcode/HLS implementations, and FS-12 should add scheduler/background orchestration.
- Domain model work includes `VideoAsset`, `VideoProcess`, `ProcessingStep`, `VideoMetadata`, `HlsResult`, lifecycle statuses, and EF mappings for video processing tables under the `files` schema.
- `UploadedKey` is intended only for upload/cancel/complete-upload flows. Processing should use `RawKey` as the source and `FinalKey`/HLS manifest output as ready content.
- Video processing skeleton lives in `backend/src/FileService/FileService.VideoProcessing`.
- Pipeline entry points are `Pipeline/ProcessingPipeline.cs`, `Pipeline/IProcessingPipeline.cs`, `Pipeline/IProcessingStepHandler.cs`, and `Pipeline/ProcessingContext.cs`.
- Step handlers are not ASP.NET handlers. They accept `ProcessingContext`, return `Result<ProcessingContext, Error>`, and receive `CancellationToken` as a method parameter.
- Registered mock steps currently cover initialize, extract metadata, generate HLS/prepare outputs, upload HLS/results, generate preview, and cleanup. They intentionally do not require FFmpeg.
- `ProcessingContext` carries `VideoAsset`, `VideoProcess`, source/final/HLS storage context through domain objects, deterministic mock working directories like `temp/video-processing/{videoAssetId}`, and domain progress/metadata via `VideoProcess`/`VideoAsset`.
- `ProcessingPipeline` loads the video asset, starts processing, creates a `VideoProcess`, executes ordered domain steps, stops on first error/cancellation/missing handler, saves failed status, and avoids leaving the asset in endless `PROCESSING`.
- Successful pipeline completion marks `VideoAsset` as `READY`; failed/cancelled execution marks `VideoAsset` as `FAILED` and stores the failure on `VideoProcess`.
- `VideoProcessingService` currently has no interface; registering it directly with `services.AddScoped<VideoProcessingService>()` is acceptable unless a concrete need for an interface appears.
- FileService transaction support was added through the infrastructure Postgres transaction manager because the pipeline depends on `Shared.Core.Database.ITransactionManager`.
- Unit orchestration tests live in `backend/tests/FileService.Domain.UnitTests/Pipeline/ProcessingPipelineTests.cs` and cover happy path, middle-step error, cancellation, wrong status/repeated launch, step order, and state transitions.
- `backend/tests/FileService.Domain.UnitTests/FileService.Domain.UnitTests.csproj` references `FileService.Core` and `FileService.VideoProcessing` for these orchestration tests.
- Last focused verification: `dotnet test "tests\FileService.Domain.UnitTests\FileService.Domain.UnitTests.csproj" --filter FullyQualifiedName~ProcessingPipelineTests` from `backend/` passed 6/6.
- Last touched-project verification: `dotnet build "src\FileService\FileService.VideoProcessing\FileService.VideoProcessing.csproj"` from `backend/` passed with only existing warnings in unrelated/newer skeleton files.
- Known user-local/unrelated change during this work: `backend/src/FileService/FileService.Web/appsettings.json` was modified and should not be reverted unless explicitly requested.
