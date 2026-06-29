# Backend Codex Guide

## Scope

This file applies to `backend/`.
More specific `AGENTS.md` files under `src/DirectoryService/` and `src/FileService/` override it there.

## Purpose

Help Codex work on the backend without opening unrelated frontend files or guessing the layer layout.

## Structure

- `DirectoryService.sln` - backend solution entry point
- `src/DirectoryService/` - directory bounded context
- `src/FileService/` - file storage bounded context
- `tests/` - integration tests
- `docker-compose.yml` - local infrastructure

## Project Layout

DirectoryService:

- `src/DirectoryService/DirectoryService.Domain` - entities, value objects, invariants
- `src/DirectoryService/DirectoryService.Contracts` - DTOs and request/response models
- `src/DirectoryService/DirectoryService.Application` - handlers, validation, abstractions, orchestration
- `src/DirectoryService/DirectoryService.Infrastructure.Postgres` - EF Core, repositories, migrations, Postgres-specific code
- `src/DirectoryService/DirectoryService.Presentation` - API host, controllers, DI, app settings

FileService:

- `src/FileService/FileService.Domain` - file-service domain concepts
- `src/FileService/FileService.Contracts` - external DTOs and contracts when needed
- `src/FileService/FileService.Core` - Minimal API slices, interfaces, storage abstractions, orchestration
- `src/FileService/FileService.Communication` - typed HTTP client for other backend services; public methods include `GetMediaAssetByIdAsync`, `GetFilesByOwnerAsync`, and `GetContentUrlAsync`
- `src/FileService/FileService.Infrastructure.Postgres` - Postgres-specific persistence
- `src/FileService/FileService.Infrastructure.S3` - S3/MinIO client and storage implementation
- `src/FileService/FileService.Web` - API host, controllers, DI, app settings

- `tests/DirectoryService.IntegrationTests` - integration tests

## Dependency Direction

- `DirectoryService.Presentation` may depend on `Application`, `Contracts`, `Domain`, `Infrastructure.Postgres`
- `Infrastructure.Postgres` may depend on `Application`, `Contracts`, `Domain`
- `Application` may depend on `Contracts`, `Domain`
- `Contracts` may depend on `Domain` only where already established
- `Domain` should remain the most independent layer
- `FileService.Web` may depend on `FileService.Core`, `Contracts`, `Domain`, and infrastructure projects.
- `FileService.Infrastructure.*` may depend on `FileService.Core`, `Contracts`, and `Domain`.
- `FileService.Core` owns Minimal API endpoint slices and interfaces for infrastructure dependencies.
- Cross-service consumers should depend on `FileService.Contracts`/`FileService.Communication`, not on `FileService.Domain` or infrastructure projects.

## Backend Rules

- Keep migrations in `Infrastructure.Postgres`.
- Keep controllers thin and move behavior into application handlers/services.
- Put DirectoryService repository interfaces in `Application` and implementations in `Infrastructure.Postgres`.
- Put FileService endpoint-facing interfaces in `FileService.Core` and implementations in infrastructure projects.
- If an API contract changes, check `Contracts`, `Presentation`, and integration tests together.
- If persistence changes, check repository code, EF configuration, and migrations together.
- For FileService upload flows, Minimal API slices in Core should call storage abstractions, not `S3Provider` directly.
- For DirectoryService attachments to FileService assets, store the public `MediaAssetId` (`Guid`) and local display/stale metadata only. Do not persist FileService storage keys, S3 object paths, presigned URLs, or FileService internal lifecycle state in DirectoryService.

## Common Commands

- Build: `dotnet build .\DirectoryService.sln`
- Test: `dotnet test .\DirectoryService.sln`
