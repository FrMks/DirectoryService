# DirectoryService Service Codex Guide

## Scope

This file applies to `backend/src/DirectoryService/`.

## Purpose

This is the main bounded context of the backend.
Use this file to keep Codex focused on the correct service layer and avoid unnecessary scanning.

## Service Layers

- `DirectoryService.Domain` - entities, invariants, value objects, core business concepts
- `DirectoryService.Contracts` - DTOs and request/response contracts
- `DirectoryService.Application` - commands, queries, handlers, validators, abstractions
- `DirectoryService.Infrastructure.Postgres` - persistence, EF Core config, migrations, repository implementations, background services
- `DirectoryService.Presentation` - controllers, host startup, DI wiring, configuration

## Layer Rules

- Keep business rules in `Domain` or `Application`, not in controllers.
- Keep transport models in `Contracts`.
- Keep Postgres and EF-specific code in `Infrastructure.Postgres`.
- Keep controllers thin and delegate to application handlers/services.
- Avoid new dependencies that break the direction defined in `backend/AGENTS.md`.

## Change Strategy

- API shape change: inspect `Contracts`, `Application`, `Presentation`, and integration tests together.
- Persistence change: inspect repositories, EF configurations, and migrations together.
- Domain model change: verify validators, mapping, and dependent handlers still align.

## FileService Attachment Notes

- DirectoryService may attach a FileService asset to a business entity by storing the public `MediaAssetId` (`Guid`) returned by FileService contracts.
- Keep FileService integration in application/orchestration code through `FileService.Communication`; do not reference FileService domain entities from DirectoryService domain code.
- DirectoryService may keep local attachment metadata for UI/degraded reads, such as file name, content type, size, asset type, and last successful check time.
- DirectoryService must not persist FileService storage keys, S3 paths, presigned/content URLs, or internal FileService lifecycle implementation details.
- Attach/replace operations should explicitly validate the asset through FileService before saving the local binding; normal entity updates should not silently overwrite attachment ids.

## Current Location Preview Attachment State

- The chosen scenario is a Location preview image backed by a FileService media asset.
- `DirectoryService.Domain/Locations/Location.cs` has nullable `PreviewMetadata`, attached through `Location.AttachPreview(...)`.
- `DirectoryService.Domain/Locations/ValueObjects/MediaAssetId.cs` wraps the external FileService asset `Guid`; DirectoryService should not generate this id itself.
- `DirectoryService.Domain/Locations/ValueObjects/LocationPreviewMetadata.cs` stores the local attachment metadata used for display/degraded reads: asset id, file name, content type, size, attached time, and last verified time.
- `DirectoryService.Infrastructure.Postgres/Configurations/LocationConfiguration.cs` maps `PreviewMetadata` columns into `locations`: `preview_asset_id`, `preview_file_name`, `preview_content_type`, `preview_size`, `preview_attached_at`, and `preview_last_verified_at`.
- Migrations `20260624045953_AddPreviewAssetidToLocation` and `20260624100128_AddLocationPreviewMetadata` cover the Location preview persistence changes.
- Explicit attach endpoint: `PUT /api/locations/{locationId:guid}/preview-asset` in `DirectoryService.Presentation/Controllers/Locations.cs`.
- Attach logic lives in `DirectoryService.Application/Locations/AttachLocationPreviewHandler.cs`; it loads the Location, calls `FileService.Communication`, validates READY/PREVIEW/image/location ownership rules, creates `LocationPreviewMetadata`, attaches it, and saves via `ITransactionManager.SaveChangesAsync`.
- Next work should add explicit replace/delete semantics if distinct from attach, then read DTO enrichment/degraded read behavior when FileService is unavailable.
