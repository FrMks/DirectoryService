# Architecture

This document describes the main backend architecture and gives AI agents a stable map for navigation. Keep implementation details in code and tests; keep this file focused on boundaries, responsibilities, and where to look first.

## Repository Shape

- `backend/` contains the .NET backend solution.
- `client/` is reserved for the frontend.
- `backend/src/DirectoryService/` contains the directory bounded context.
- `backend/src/FileService/` contains the file storage and media bounded context.
- `backend/tests/` contains backend tests.

## FileService Overview

FileService owns file upload, storage, media asset lifecycle, public file contracts, and video processing orchestration. Other services should interact with FileService through HTTP/transport contracts, not by referencing FileService domain or infrastructure internals.

FileService is split into projects by responsibility:

- `FileService.Domain` - domain entities, value objects, lifecycle statuses, and invariants.
- `FileService.Contracts` - public DTOs and transport contracts.
- `FileService.Communication` - typed HTTP client for other backend services.
- `FileService.Core` - Minimal API endpoint slices, use-case orchestration, interfaces required by endpoints, and storage abstractions.
- `FileService.Infrastructure.Postgres` - EF Core persistence, repository implementations, transactions, and migrations.
- `FileService.Infrastructure.S3` - S3/MinIO storage implementation.
- `FileService.VideoProcessing` - video processing pipeline skeleton, shared context, and mock step handlers.
- `FileService.Web` - API host, DI wiring, middleware pipeline, and endpoint mapping.

See FileService edge cases in `backend/src/FileService/edge-cases.md`.

## DirectoryService Overview

DirectoryService is the main directory bounded context. It owns the organization's directory model, API surface for directory entities, application handlers, validation, and Postgres persistence for directory data.

DirectoryService is split into projects by responsibility:

- `DirectoryService.Domain` - entities, value objects, invariants, and core business concepts.
- `DirectoryService.Contracts` - DTOs and request/response transport contracts.
- `DirectoryService.Application` - commands, queries, handlers, validators, repository abstractions, and application orchestration.
- `DirectoryService.Infrastructure.Postgres` - EF Core persistence, repository implementations, migrations, and Postgres-specific background services.
- `DirectoryService.Presentation` - API host, controllers, DI wiring, middleware, and configuration.

See DirectoryService edge cases in `backend/src/DirectoryService/edge-cases.md`.

## DirectoryService Dependency Direction

- `DirectoryService.Domain` should remain independent from application, infrastructure, presentation, and FileService internals.
- `DirectoryService.Contracts` owns transport shapes and should not contain business logic.
- `DirectoryService.Application` coordinates use cases, validation, repository abstractions, transactions, and cross-service communication.
- `DirectoryService.Infrastructure.Postgres` implements persistence concerns and owns EF-specific code.
- `DirectoryService.Presentation` should keep controllers thin and delegate behavior to application handlers/services.

## DirectoryService Domain Areas

DirectoryService domain code represents the directory structure and related business concepts. Typical areas include:

- departments and hierarchy rules.
- locations and location metadata.
- positions and position-related directory data.
- value objects used to protect invariants.
- local references to external FileService assets where needed.

Domain model changes should be checked together with validators, handlers, mappings, migrations, and integration tests.

## DirectoryService Application Layer

`DirectoryService.Application` owns use-case behavior. Handlers should load domain objects, enforce application-level validation, call external services through abstractions, use domain methods for state changes, and save through transaction/repository abstractions.

When an API shape changes, inspect these together:

- `DirectoryService.Contracts`
- `DirectoryService.Application`
- `DirectoryService.Presentation`
- relevant integration tests

## DirectoryService.Infrastructure.Postgres

This project owns DirectoryService persistence implementation:

- EF Core DbContext and entity configurations.
- repository implementations.
- migrations.
- Postgres-specific services.

When persistence changes, update repository queries, EF configuration, migrations, and tests together.

## DirectoryService.Presentation

`DirectoryService.Presentation` is the host/controller layer. It should stay focused on:

- controller routing and request/response glue.
- DI composition.
- middleware.
- configuration.

Business rules should not be implemented in controllers. Controllers should delegate to application handlers/services.

## DirectoryService And FileService Attachments

DirectoryService may attach a FileService asset to a business entity by storing the public `MediaAssetId` (`Guid`) returned by FileService contracts.

Current established scenario:

- Location preview image backed by a FileService media asset.
- `Location.PreviewMetadata` stores local display/degraded metadata.
- `MediaAssetId` wraps the external FileService asset id.
- Explicit attach endpoint: `PUT /api/locations/{locationId:guid}/preview-asset`.
- Attach logic validates the asset through `FileService.Communication` before saving the local binding.

DirectoryService must not persist FileService storage keys, S3 paths, presigned/content URLs, or FileService internal lifecycle details.

## FileService Dependency Direction

- `FileService.Domain` should remain independent from infrastructure and web concerns.
- `FileService.Contracts` contains public transport shapes and should avoid leaking storage internals.
- `FileService.Core` may depend on Domain and Contracts. It defines abstractions for storage/persistence-facing operations used by endpoints.
- `FileService.Infrastructure.Postgres` implements persistence interfaces and owns database-specific code.
- `FileService.Infrastructure.S3` implements storage abstractions and owns S3/MinIO details.
- `FileService.VideoProcessing` depends on domain/core abstractions needed for orchestration, but its pipeline steps must not be ASP.NET endpoint handlers.
- `FileService.Web` composes the application by registering services, infrastructure, endpoints, and middleware.

## Public Boundary

`MediaAsset.Id` is the public asset identifier exposed to other services. External consumers should use `FileService.Contracts` and `FileService.Communication`.

Do not expose or persist FileService internals outside FileService:

- S3 bucket/object paths.
- `UploadedKey`, `RawKey`, `FinalKey`, or other storage keys.
- Presigned URLs as long-lived data.
- Internal lifecycle state that belongs to FileService only.

`GetContentUrlResponse.Url` is short-lived and should not be stored by consumers.

## Upload And Storage Flow

File upload flows are implemented through Minimal API slices in `FileService.Core`. They should call storage abstractions, not concrete S3 classes directly.

Storage details belong in `FileService.Infrastructure.S3`. PostgreSQL persistence details belong in `FileService.Infrastructure.Postgres`.

Storage key lifecycle convention:

- `UploadedKey` is for upload/cancel/complete-upload flows.
- `RawKey` is the source object used by processing.
- `FinalKey` is the ready object or manifest used for consumption/playback.

## FileService.Domain

The domain layer owns media concepts and lifecycle rules. Important concepts include:

- `MediaAsset` - base media asset state.
- `VideoAsset` - video-specific asset state and processing lifecycle.
- `PreviewAsset` - preview/thumbnail-related asset state.
- `MediaData`, `FileName`, `ContentType`, `MediaOwner`, `StorageKey` - value objects.
- `VideoProcess` and `ProcessingStep` - domain model for video processing progress and step state.
- `VideoMetadata` and `HlsResult` - video output/metadata value objects.

Domain methods should enforce valid status transitions. Application, pipeline, and infrastructure code should use domain methods rather than setting lifecycle state directly.

## FileService.Core

`FileService.Core` owns endpoint-facing use cases and interfaces required by those use cases. Minimal API slices should stay thin and delegate storage, persistence, and domain behavior to abstractions/domain objects.

Typical responsibilities:

- start upload
- complete upload
- multipart upload operations
- cancel pending upload
- get content/download URLs
- get media assets by owner or id
- define storage/provider abstractions used by infrastructure

Expected error mapping:

- Validation errors -> 400
- NotFound errors -> 404
- Conflict errors -> 409
- unexpected Failure errors -> 500

Expected bad requests should not be represented as generic internal failures.

## FileService.Infrastructure.Postgres

This project owns FileService persistence implementation:

- EF Core DbContext and configurations.
- repository implementations for media/video processing data.
- migrations.
- transaction manager implementation for `Shared.Core.Database.ITransactionManager`.

Keep database schema changes, EF mappings, and migrations together.

## FileService.Infrastructure.S3

This project owns object storage implementation:

- S3/MinIO client setup.
- upload URL generation.
- multipart upload operations.
- metadata lookup.
- object deletion/abort behavior.

S3 details should not leak into contracts, DirectoryService, or frontend code.

## FileService.VideoProcessing

`FileService.VideoProcessing` is responsible for video processing orchestration. It coordinates ordered processing steps and domain state transitions, but it does not own HTTP endpoints, concrete storage implementations, or background scheduling.

Current scope is FS-10 skeleton work. It intentionally does not require real FFmpeg. Real ffprobe/transcode/HLS implementation belongs to FS-11. Background/scheduler execution belongs to FS-12.

Main components:

- `VideoProcessingService` - application-facing service for starting video processing from another layer. It can be registered directly with `services.AddScoped<VideoProcessingService>()` while it has no interface.
- `IProcessingPipeline` - pipeline abstraction.
- `ProcessingPipeline` - orchestrator that loads the asset, starts processing, creates/updates `VideoProcess`, runs steps in order, saves progress/failure, and prevents endless `PROCESSING` state.
- `ProcessingContext` - per-run context shared by steps. It carries the loaded `VideoAsset`, active `VideoProcess`, storage context via domain objects, working directories, and accumulated metadata/progress.
- `IProcessingStepHandler` - one pipeline step contract. Steps accept `ProcessingContext`, return `Result<ProcessingContext, Error>`, and accept a `CancellationToken` parameter.
- mock step handlers - temporary in-memory/mock FS-10 steps for metadata extraction, output/HLS preparation, upload, preview, and cleanup.

Pipeline behavior:

- start processing moves the video asset into `PROCESSING`.
- successful completion moves the video asset into `READY`.
- any step failure, missing handler, or cancellation moves the video asset into `FAILED` and records the reason on `VideoProcess`.
- steps execute in domain order and stop on the first failure.
- pipeline steps must be testable without a web host.

Do not mix scheduler concerns into `ProcessingPipeline`. A scheduler/background worker may call `VideoProcessingService` later, but the pipeline should remain an orchestration component.

Tests for the pipeline live in `backend/tests/FileService.Domain.UnitTests/Pipeline/ProcessingPipelineTests.cs`.

Focused verification command from `backend/`:

```powershell
dotnet test "tests\FileService.Domain.UnitTests\FileService.Domain.UnitTests.csproj" --filter FullyQualifiedName~ProcessingPipelineTests
```

## FileService.Web

`FileService.Web` is the host layer. It should stay focused on:

- dependency injection composition.
- middleware.
- endpoint mapping.
- configuration.

Business rules should not be implemented in `Program.cs` or host startup code.

## Cross-Service Rules

DirectoryService may store FileService public asset ids and display/stale metadata where needed, but it must not store FileService storage keys, S3 paths, presigned URLs, or internal lifecycle state.

The frontend should call backend HTTP APIs and must not reference backend projects directly.

## Useful Commands

From repository root:

```powershell
dotnet build .\backend\DirectoryService.sln
dotnet test .\backend\DirectoryService.sln
```

From `backend/`:

```powershell
dotnet build "src\FileService\FileService.VideoProcessing\FileService.VideoProcessing.csproj"
dotnet test "tests\FileService.Domain.UnitTests\FileService.Domain.UnitTests.csproj" --filter FullyQualifiedName~ProcessingPipelineTests
```
