# DirectoryService

Project layout:

- `backend/` - .NET backend for the directory service
- `client/` - reserved for the future Next.js frontend
- `AGENTS.md` files describe repository/layer rules for AI agents. Read the closest one before editing files in a folder.

Backend solution:

- `backend/DirectoryService.sln`
- DirectoryService API entry point: `backend/src/DirectoryService/DirectoryService.Presentation/Program.cs`
- FileService API entry point: `backend/src/FileService/FileService.Web/Program.cs`

Backend map for AI agents:

- `backend/src/DirectoryService/` - directory bounded context.
- `backend/src/FileService/` - file storage and media bounded context.
- `backend/tests/` - backend tests.
- Keep backend business rules in backend domain/application/core layers. The client must consume backend over HTTP and must not reference backend code directly.
- Architecture overview: `architecture.md`.
- FileService edge-case checklist: `backend/src/FileService/edge-cases.md`.
- DirectoryService edge-case checklist: `backend/src/DirectoryService/edge-cases.md`.

FileService map:

- `FileService.Domain` - file/media domain entities, value objects, status transitions, and invariants.
- `FileService.Core` - Minimal API endpoint slices, orchestration-facing interfaces, and storage abstractions.
- `FileService.Infrastructure.Postgres` - EF Core persistence, repositories, transactions, migrations.
- `FileService.Infrastructure.S3` - S3/MinIO implementation details.
- `FileService.VideoProcessing` - video processing orchestration service, pipeline skeleton, and mock processing steps.
- `FileService.Web` - API host, DI composition, middleware, endpoint mapping.

FileService.VideoProcessing:

- `FileService.VideoProcessing` is responsible for video processing orchestration. It coordinates the domain state transitions and ordered processing steps, but it does not own HTTP endpoints, storage provider implementation details, or background scheduling.
- `VideoProcessingService` is the application-facing service for starting video processing from another layer. It should delegate orchestration to `IProcessingPipeline` and keep scheduler/background concerns out of the pipeline itself.
- `ProcessingPipeline` is the orchestrator. It loads the video asset, creates/updates the processing domain state, executes steps in order, persists progress/failure, and guarantees the video is not left in endless `PROCESSING` after an error.
- `ProcessingContext` is the shared per-run state passed between pipeline steps. It should carry the loaded video asset/process, storage context through domain objects, working directories, and accumulated metadata/progress.
- `IProcessingStepHandler` is the contract for one pipeline step. Step implementations should be technology-specific only at the edge and should not know about ASP.NET endpoints.
- Mock step handlers are temporary FS-10 implementations. They make orchestration testable before real FFmpeg/ffprobe/HLS work is added.

Video Processing branch notes:

- Current video processing work is FS-9/FS-10 groundwork. It intentionally has no mandatory real FFmpeg dependency yet; FS-11 should replace mock steps with ffprobe/transcode/HLS steps, and FS-12 should add scheduler/background execution.
- Domain concepts include `VideoAsset`, `VideoProcess`, `ProcessingStep`, `VideoMetadata`, `HlsResult`, processing statuses, and EF mappings for video processing tables under the `files` schema.
- Storage lifecycle convention: use `UploadedKey` only for upload/cancel/complete-upload flows; processing source should be `RawKey`; ready playback/content should use `FinalKey`/HLS manifest output.
- Pipeline skeleton lives in `backend/src/FileService/FileService.VideoProcessing/`.
- Key pipeline files: `Pipeline/ProcessingPipeline.cs`, `Pipeline/IProcessingPipeline.cs`, `Pipeline/IProcessingStepHandler.cs`, and `Pipeline/ProcessingContext.cs`.
- Pipeline steps are not ASP.NET handlers. They should be easy to unit test without a web host, accept `ProcessingContext`, return `Result<ProcessingContext, Error>`, and accept `CancellationToken` as an execution parameter.
- `ProcessingContext` carries the loaded `VideoAsset`, the active `VideoProcess`, storage context through domain objects, deterministic working directories such as `temp/video-processing/{videoAssetId}`, and accumulated metadata/progress through domain state.
- `ProcessingPipeline` loads the asset, starts processing, creates the process, runs ordered steps, stops on the first failure/cancellation/missing handler, saves failure state, and must not leave the asset stuck in endless `PROCESSING`.
- Successful completion marks the video asset `READY`; failed/cancelled execution marks it `FAILED` and records the reason on `VideoProcess`.
- Mock steps currently cover initialize, extract metadata, generate HLS/prepare outputs, upload HLS/results, generate preview, and cleanup. Keep temporary file ownership clear because real cleanup will be added later.
- Do not mix scheduler/background concerns into the pipeline. Scheduler/background launch belongs to a later layer/task.
- `VideoProcessingService` currently has no interface. Direct DI registration with `services.AddScoped<VideoProcessingService>()` is acceptable unless a concrete need for an interface appears.
- FileService transaction support is provided by infrastructure Postgres through `Shared.Core.Database.ITransactionManager`.
- Orchestration tests live in `backend/tests/FileService.Domain.UnitTests/Pipeline/ProcessingPipelineTests.cs` and cover happy path, middle-step error, cancellation, wrong status/repeated launch, step order, and state transitions.
- `backend/tests/FileService.Domain.UnitTests/FileService.Domain.UnitTests.csproj` references `FileService.Core` and `FileService.VideoProcessing` for these tests.

Useful backend commands:

- Build backend solution from repository root: `dotnet build .\backend\DirectoryService.sln`
- Test backend solution from repository root: `dotnet test .\backend\DirectoryService.sln`
- Build FileService video processing from `backend/`: `dotnet build "src\FileService\FileService.VideoProcessing\FileService.VideoProcessing.csproj"`
- Run video processing orchestration tests from `backend/`: `dotnet test "tests\FileService.Domain.UnitTests\FileService.Domain.UnitTests.csproj" --filter FullyQualifiedName~ProcessingPipelineTests`

Graphify code graph:

- Generated output path: `backend/graphify-out/`
- For broad dependency/codebase analysis, agents should first inspect `backend/graphify-out/graph.json` and `backend/graphify-out/.graphify_analysis.json` before doing wide source scans.
- Regenerate after large structural changes from `backend/` with `graphify . --code-only`.
- `backend/graphify-out/` is generated output and is ignored by git.

After this structure, you can create the frontend from the repository root with:

```bash
npx create-next-app@latest client
```
